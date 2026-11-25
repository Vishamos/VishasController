// VishaSynapse 대역 테스트 서버.
// 실제 서버에 41 등록·치지직 전달이 들어오기 전에 컨트롤러를 시험하려고 만든다.
// 규칙은 VishaSynapse docs/protocol/README.md §3·§5a를 따른다.
//
// 실행: dotnet run --project tools/TestServer [-- --port 4000] [--no41]
//   --no41 : 시그니처 41을 모르는 지금 서버처럼 404로 응답한다

using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

int port = 4000;
bool no41 = false;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--port" && i + 1 < args.Length) port = int.Parse(args[++i]);
    else if (args[i] == "--no41") no41 = true;
}

var clients = new ConcurrentDictionary<int, Client>();
int nextIndex = 0;
string chzzkState = "connected";
var rng = new Random();
CancellationTokenSource? autoCts = null;

var listener = new HttpListener();
listener.Prefixes.Add($"http://localhost:{port}/");
try { listener.Start(); }
catch (HttpListenerException ex)
{
    Console.WriteLine($"포트 {port}를 열 수 없습니다: {ex.Message}");
    Console.WriteLine("VishaSynapse 서버가 같은 포트를 쓰고 있다면 --port 4001 처럼 다른 포트로 실행하세요.");
    return 1;
}

Log($"테스트 서버 시작: ws://localhost:{port}  (41 {(no41 ? "미지원 모드" : "지원")})");
PrintHelp();
_ = AcceptLoop();

// ── 콘솔 명령 ───────────────────────────────────────────
while (Console.ReadLine() is { } line)
{
    var parts = line.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0) continue;
    var arg = parts.Length > 1 ? parts[1] : "";
    switch (parts[0])
    {
        case "c": await SetState("connected"); break;
        case "d": await SetState("disconnected"); break;
        case "a": await SetState("auth_required"); break;
        case "x": await SetState("disabled"); break;
        case "m": await SendChat(RandomNick(), "common_user", arg.Length > 0 ? arg : "테스트 채팅입니다"); break;
        case "s": await SendChat("비샤모스", "streamer", arg.Length > 0 ? arg : "스트리머 채팅"); break;
        case "g": await SendChat("매니저", "streaming_chat_manager", arg.Length > 0 ? arg : "채팅 관리자 채팅"); break;
        case "e": await SendChat(RandomNick(), "common_user", "이모지 {:d_1:} 테스트 {:d_2:}",
                      new() { ["d_1"] = "https://example.com/d_1.png", ["d_2"] = "https://example.com/d_2.png" }); break;
        case "b":
            int n = int.TryParse(arg, out var k) ? k : 20;
            for (int j = 0; j < n; j++) await SendChat(RandomNick(), "common_user", $"몰아치는 채팅 {j + 1}/{n}");
            break;
        case "auto":
            if (autoCts == null) { autoCts = new(); _ = AutoChat(autoCts.Token); Log("자동 채팅 시작 (2초마다)"); }
            else { autoCts.Cancel(); autoCts = null; Log("자동 채팅 중지"); }
            break;
        case "u": await Broadcast("""{"to":41,"from":101,"type":"chzzk.donation","data":{"amount":1000}}"""); Log("모르는 type 보냄"); break;
        case "big": await Broadcast(BigChat()); Log("64KB 넘는 메시지 보냄"); break;
        case "kick":
            foreach (var cl in clients.Values) cl.Socket.Abort();
            Log("모든 클라이언트 강제로 끊음");
            break;
        case "list": Log($"등록된 클라이언트 {clients.Count}개: {string.Join(", ", clients.Keys)}"); break;
        case "sleep": await Task.Delay(TimeSpan.FromSeconds(double.Parse(arg))); break;
        case "q": listener.Stop(); return 0;
        default: PrintHelp(); break;
    }
}
return 0;

// ── 접속 처리 ───────────────────────────────────────────
async Task AcceptLoop()
{
    while (listener.IsListening)
    {
        HttpListenerContext ctx;
        try { ctx = await listener.GetContextAsync(); }
        catch { break; }

        if (!ctx.Request.IsWebSocketRequest)
        {
            ctx.Response.StatusCode = 400;
            ctx.Response.Close();
            continue;
        }
        _ = HandleClient(ctx);
    }
}

async Task HandleClient(HttpListenerContext ctx)
{
    var wsCtx = await ctx.AcceptWebSocketAsync(null, TimeSpan.FromSeconds(30));
    var client = new Client(wsCtx.WebSocket, Interlocked.Increment(ref nextIndex));
    Log($"#{client.Index} 접속 ({ctx.Request.RemoteEndPoint})");

    // 30초 안에 등록하지 않으면 끊는다 (실제 서버와 같은 규칙)
    _ = Task.Delay(30_000).ContinueWith(async _ =>
    {
        if (!client.Registered && client.Socket.State == WebSocketState.Open)
        {
            Log($"#{client.Index} 등록 시간 초과 → 1008");
            try { await client.Socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "register_timeout", default); } catch { }
        }
    });

    var buffer = new byte[64 * 1024];
    try
    {
        while (client.Socket.State == WebSocketState.Open)
        {
            var r = await client.Socket.ReceiveAsync(buffer, default);
            if (r.MessageType == WebSocketMessageType.Close)
            {
                await client.Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", default);
                break;
            }
            if (r.MessageType != WebSocketMessageType.Text || !r.EndOfMessage) continue;

            var text = Encoding.UTF8.GetString(buffer, 0, r.Count);
            Log($"#{client.Index} 받음: {text}");
            await OnMessage(client, text);
        }
    }
    catch (Exception ex) { Log($"#{client.Index} 오류: {ex.Message}"); }
    finally
    {
        clients.TryRemove(client.Index, out _);
        Log($"#{client.Index} 연결 종료");
        client.Socket.Dispose();
    }
}

async Task OnMessage(Client client, string text)
{
    try
    {
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        var type = root.GetProperty("type").GetString();
        var from = root.GetProperty("from").GetInt32();

        if (type == "client.register" && !client.Registered)
        {
            if (from != 41 || no41)
            {
                await client.Send("""{"from":0,"type":"error","data":{"code":404,"message":"not_found"}}""");
                Log($"#{client.Index} 등록 거부 (from={from}) → 404");
                return;
            }
            client.Registered = true;
            clients[client.Index] = client;
            await client.Send($$$"""{"to":41,"from":0,"type":"client.registered","data":{"index":{{{client.Index}}}}}""");
            await client.Send(StatusJson());   // 등록 직후 현재 상태 한 번
            Log($"#{client.Index} 41로 등록, 상태 {chzzkState} 보냄");
        }
        else if (!client.Registered)
        {
            await client.Send("""{"from":0,"type":"error","data":{"code":401,"message":"not_registered"}}""");
        }
    }
    catch (Exception)
    {
        await client.Send("""{"from":0,"type":"error","data":{"code":400,"message":"bad_request"}}""");
    }
}

// ── 보내기 ─────────────────────────────────────────────
async Task SetState(string state)
{
    chzzkState = state;
    await Broadcast(StatusJson());
    Log($"치지직 상태 → {state}");
}

string StatusJson() =>
    $$$"""{"to":41,"from":101,"type":"chzzk.status","data":{"state":"{{{chzzkState}}}"}}""";

async Task SendChat(string nick, string role, string content, Dictionary<string, string>? emojis = null)
{
    var json = JsonSerializer.Serialize(new
    {
        to = 41,
        from = 101,
        type = "chzzk.chat",
        data = new
        {
            nickname = nick,
            role,
            content,
            emojis = emojis ?? new Dictionary<string, string>(),
            time = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
        },
    }, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    await Broadcast(json);
    Log($"채팅 보냄 → {clients.Count}개: [{role}] {nick}: {content}");
}

async Task Broadcast(string json)
{
    foreach (var c in clients.Values)
        await c.Send(json);
}

async Task AutoChat(CancellationToken ct)
{
    string[] lines = { "안녕하세요", "ㅋㅋㅋㅋ", "오늘 방송 뭐 해요?", "{:d_1:}", "굿굿", "와 대박" };
    try
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(2000, ct);
            await SendChat(RandomNick(), "common_user", lines[rng.Next(lines.Length)]);
        }
    }
    catch (OperationCanceledException) { }
}

string RandomNick()
{
    string[] nicks = { "시청자1", "고양이", "밤하늘", "치즈냥", "Visha팬", "지나가던사람" };
    return nicks[rng.Next(nicks.Length)];
}

string BigChat() =>
    $$$"""{"to":41,"from":101,"type":"chzzk.chat","data":{"nickname":"큰메시지","role":"common_user","content":"{{{new string('A', 70_000)}}}","emojis":{},"time":0}}""";

void PrintHelp() => Console.WriteLine("""
    명령: c 연결됨 | d 끊김 | a 재로그인 필요 | x 연동 꺼짐   (chzzk.status)
          m [내용] 일반 채팅 | s [내용] 스트리머 | g [내용] 채팅 관리자 | e 이모지 채팅
          b [개수] 채팅 몰아 보내기 | auto 자동 채팅 켜기/끄기
          u 모르는 type | big 64KB 초과 | kick 강제 끊기 | list | sleep 초 | q 종료
    """);

static void Log(string msg) => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");

sealed class Client(WebSocket socket, int index)
{
    readonly SemaphoreSlim _sendLock = new(1, 1);
    public WebSocket Socket { get; } = socket;
    public int Index { get; } = index;
    public bool Registered { get; set; }

    public async Task Send(string json)
    {
        await _sendLock.WaitAsync();
        try
        {
            if (Socket.State == WebSocketState.Open)
                await Socket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, default);
        }
        catch { /* 끊긴 클라이언트 */ }
        finally { _sendLock.Release(); }
    }
}

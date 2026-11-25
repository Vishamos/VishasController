using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace VishasController.Net;

/// <summary>
/// VishaSynapse 서버에 시그니처 41로 접속해 메시지를 받는다(보내는 것은 등록뿐).
/// 끊기면 스스로 다시 접속·등록한다. 이벤트는 생성한 스레드(UI)의 SynchronizationContext로 넘긴다.
/// </summary>
public sealed class SynapseClient
{
    public const int Signature = 41;
    const int MaxMessageBytes = 64 * 1024;                                  // 서버 한도 (protocol §8)
    static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);
    static readonly TimeSpan RegisterTimeout = TimeSpan.FromSeconds(10);   // 서버 한도 30초보다 짧게
    static readonly TimeSpan MinRetry = TimeSpan.FromSeconds(1);
    static readonly TimeSpan MaxRetry = TimeSpan.FromSeconds(30);
    static readonly TimeSpan UnsupportedRetry = TimeSpan.FromSeconds(60);

    public event Action<LinkState, string>? LinkStateChanged;   // (단계, 설명)
    public event Action<ChzzkChat>? ChatReceived;
    public event Action<ChzzkState>? ChzzkStateChanged;
    public event Action<int, string>? ServerError;              // (code, message)

    readonly SynchronizationContext _ui;
    readonly Uri _uri;
    CancellationTokenSource? _cts;
    Task? _loop;

    public SynapseClient(Uri uri, SynchronizationContext ui)
    {
        _uri = uri;
        _ui = ui;
    }

    public Uri Uri => _uri;

    public void Start()
    {
        if (_loop != null) return;
        _cts = new CancellationTokenSource();
        _loop = Task.Run(() => RunAsync(_cts.Token));
    }

    public async Task StopAsync()
    {
        if (_cts == null || _loop == null) return;
        _cts.Cancel();
        try { await _loop.ConfigureAwait(false); } catch { /* 종료 중 예외는 무시 */ }
        _cts.Dispose();
        _cts = null;
        _loop = null;
        Post(() => LinkStateChanged?.Invoke(LinkState.Stopped, ""));
    }

    // ── 재접속 루프 ─────────────────────────────────────────

    async Task RunAsync(CancellationToken ct)
    {
        var retry = MinRetry;
        while (!ct.IsCancellationRequested)
        {
            var result = await RunOnceAsync(ct).ConfigureAwait(false);
            if (ct.IsCancellationRequested) break;

            if (result.WasRegistered) retry = MinRetry;   // 한 번이라도 등록됐으면 대기 시간 초기화

            TimeSpan wait;
            if (result.Unsupported)
            {
                wait = UnsupportedRetry;
                RaiseLink(LinkState.Unsupported, $"서버가 컨트롤러(41)를 지원하지 않음 · {wait.TotalSeconds:0}초 후 다시 시도");
            }
            else
            {
                wait = retry;
                RaiseLink(LinkState.Waiting, $"{result.Reason} · {wait.TotalSeconds:0}초 후 다시 접속");
                retry = TimeSpan.FromTicks(Math.Min(retry.Ticks * 2, MaxRetry.Ticks));
            }

            try { await Task.Delay(wait, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }
    }

    readonly record struct SessionResult(bool WasRegistered, bool Unsupported, string Reason);

    /// <summary>한 번 접속해서 끊길 때까지 받는다.</summary>
    async Task<SessionResult> RunOnceAsync(CancellationToken ct)
    {
        using var ws = new ClientWebSocket();
        // 서버가 30초마다 ping을 보내고 ClientWebSocket이 pong을 자동으로 보낸다.
        // 반대로 서버가 말없이 사라진 경우를 잡으려고 우리도 ping을 보내고 응답을 기다린다(.NET 9+).
        ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
        ws.Options.KeepAliveTimeout = TimeSpan.FromSeconds(30);

        bool registered = false;
        try
        {
            RaiseLink(LinkState.Connecting, _uri.ToString());
            using (var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                connectCts.CancelAfter(ConnectTimeout);
                await ws.ConnectAsync(_uri, connectCts.Token).ConfigureAwait(false);
            }

            RaiseLink(LinkState.Registering, "");
            var reg = Encoding.UTF8.GetBytes(MessageParser.RegisterJson(Signature));
            await ws.SendAsync(reg, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);

            // 등록 응답이 제때 오지 않으면 연결을 끊는다.
            _ = Task.Delay(RegisterTimeout, ct).ContinueWith(_ =>
            {
                if (!Volatile.Read(ref registered)) try { ws.Abort(); } catch { }
            }, TaskScheduler.Default);

            var buffer = new byte[8 * 1024];
            while (true)
            {
                var text = await ReceiveTextAsync(ws, buffer, ct).ConfigureAwait(false);
                if (text == null)
                {
                    var why = ws.CloseStatus is { } s ? $"서버가 연결을 닫음 ({(int)s} {ws.CloseStatusDescription})" : "연결 끊김";
                    return new(registered, false, why);
                }
                if (text.Length == 0) continue; // 바이너리·크기 초과 메시지는 버림

                switch (Handle(text, ref registered))
                {
                    case HandleResult.Unsupported:
                        await CloseQuietlyAsync(ws).ConfigureAwait(false);
                        return new(false, true, "");
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await CloseQuietlyAsync(ws).ConfigureAwait(false);
            return new(registered, false, "종료");
        }
        catch (OperationCanceledException)
        {
            return new(registered, false, "접속 시간 초과");
        }
        catch (WebSocketException ex)
        {
            return new(registered, false, registered || ws.State != WebSocketState.Aborted
                ? Describe(ex) : "등록 응답 없음");
        }
        catch (Exception ex)
        {
            return new(registered, false, ex.Message);
        }
    }

    static string Describe(WebSocketException ex) =>
        ex.InnerException?.Message ?? ex.Message;

    enum HandleResult { Ok, Unsupported }

    HandleResult Handle(string text, ref bool registered)
    {
        JsonDocument doc;
        try { doc = JsonDocument.Parse(text); }
        catch (JsonException) { return HandleResult.Ok; }   // 깨진 메시지는 버림

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return HandleResult.Ok;
            root.TryGetProperty("data", out var data);

            switch (MessageParser.GetString(root, "type"))
            {
                case "client.registered":
                    Volatile.Write(ref registered, true);
                    var index = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("index", out var i) ? i.ToString() : "?";
                    RaiseLink(LinkState.Registered, $"연결 번호 {index}");
                    break;

                case "error":
                    int code = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("code", out var c) && c.TryGetInt32(out var n) ? n : 0;
                    var message = MessageParser.GetString(data, "message");
                    // 등록 전 404 = 서버가 시그니처 41을 모름
                    if (!registered && code == 404) return HandleResult.Unsupported;
                    Post(() => ServerError?.Invoke(code, message));
                    break;

                case "chzzk.chat":
                    if (data.ValueKind == JsonValueKind.Object)
                    {
                        var chat = MessageParser.ParseChat(data);
                        Post(() => ChatReceived?.Invoke(chat));
                    }
                    break;

                case "chzzk.status":
                    var state = MessageParser.ParseState(MessageParser.GetString(data, "state"));
                    Post(() => ChzzkStateChanged?.Invoke(state));
                    break;

                // 모르는 type은 무시 (나중에 후원 등 추가 가능)
            }
        }
        return HandleResult.Ok;
    }

    /// <summary>텍스트 메시지 하나를 받는다. 닫히면 null, 버릴 메시지면 "".</summary>
    static async Task<string?> ReceiveTextAsync(ClientWebSocket ws, byte[] buffer, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        bool tooBig = false;
        WebSocketReceiveResult r;
        do
        {
            r = await ws.ReceiveAsync(buffer, ct).ConfigureAwait(false);
            if (r.MessageType == WebSocketMessageType.Close) return null;
            if (!tooBig)
            {
                if (ms.Length + r.Count > MaxMessageBytes) { tooBig = true; ms.SetLength(0); }
                else ms.Write(buffer, 0, r.Count);
            }
        } while (!r.EndOfMessage);

        if (tooBig || r.MessageType != WebSocketMessageType.Text) return "";
        return Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
    }

    static async Task CloseQuietlyAsync(ClientWebSocket ws)
    {
        if (ws.State != WebSocketState.Open) return;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cts.Token).ConfigureAwait(false);
        }
        catch { /* 이미 끊긴 경우 */ }
    }

    void RaiseLink(LinkState state, string detail) => Post(() => LinkStateChanged?.Invoke(state, detail));

    void Post(Action action) => _ui.Post(_ => action(), null);
}

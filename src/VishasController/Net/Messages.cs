using System.Text.Json;

namespace VishasController.Net;

/// <summary>서버(VishaSynapse)와의 연결 단계.</summary>
public enum LinkState
{
    Stopped,      // 시작 전 또는 종료됨
    Connecting,   // WebSocket 접속 중
    Registering,  // client.register를 보내고 응답 대기
    Registered,   // 등록 완료, 메시지 수신 중
    Waiting,      // 끊김. 잠시 후 다시 접속
    Unsupported,  // 서버가 시그니처 41을 모름(404). 길게 기다렸다가 다시 시도
}

/// <summary>chzzk.status의 state. docs/protocol/README.md §5a.</summary>
public enum ChzzkState
{
    Unknown,       // 아직 받지 못함(서버 미연결 포함) 또는 모르는 값
    Disabled,      // 서버가 치지직 연동을 하지 않는 중. 서버를 다시 켜야 바뀐다
    Connected,     // 채팅을 받는 중
    Disconnected,  // 서버가 치지직에 재연결 중. 이 동안의 채팅은 받지 못한다
    AuthRequired,  // 실행 PC에서 치지직 재로그인 필요
}

/// <summary>chzzk.chat의 data.</summary>
public sealed record ChzzkChat(
    string Nickname,
    string Role,
    string Content,
    IReadOnlyDictionary<string, string> Emojis,
    DateTimeOffset Time)
{
    /// <summary>표시용 역할 이름. 일반 시청자는 빈 문자열.</summary>
    public string RoleLabel => Role switch
    {
        "streamer" => "스트리머",
        "streaming_channel_manager" => "채널 관리자",
        "streaming_chat_manager" => "채팅 관리자",
        _ => "",
    };

    /// <summary>{:코드:} 이모지를 :코드: 로 바꾼 표시용 내용. (이미지 표시는 나중 단계)</summary>
    public string DisplayContent => Content.Replace("{:", ":").Replace(":}", ":");
}

/// <summary>서버 JSON 메시지 해석. 모르는 type·키는 무시한다.</summary>
public static class MessageParser
{
    public static string RegisterJson(int signature) =>
        $$"""{"to":0,"from":{{signature}},"type":"client.register","data":""}""";

    public static ChzzkState ParseState(string? value) => value switch
    {
        "disabled" => ChzzkState.Disabled,
        "connected" => ChzzkState.Connected,
        "disconnected" => ChzzkState.Disconnected,
        "auth_required" => ChzzkState.AuthRequired,
        _ => ChzzkState.Unknown,
    };

    public static ChzzkChat ParseChat(JsonElement data)
    {
        var emojis = new Dictionary<string, string>();
        if (data.TryGetProperty("emojis", out var e) && e.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in e.EnumerateObject())
                if (p.Value.ValueKind == JsonValueKind.String)
                    emojis[p.Name] = p.Value.GetString()!;
        }

        var time = DateTimeOffset.Now;
        if (data.TryGetProperty("time", out var t) && t.TryGetInt64(out var ms))
            time = DateTimeOffset.FromUnixTimeMilliseconds(ms).ToLocalTime();

        return new ChzzkChat(
            GetString(data, "nickname"),
            GetString(data, "role"),
            GetString(data, "content"),
            emojis,
            time);
    }

    public static string GetString(JsonElement obj, string key) =>
        obj.ValueKind == JsonValueKind.Object &&
        obj.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()!
            : "";
}

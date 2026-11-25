using System.Text.Json;

namespace VishasController;

/// <summary>
/// 사용자 설정. %AppData%\VishasController\settings.json 에 저장한다.
/// 서버 주소(LAN IP)는 공개 저장소에 올리지 않도록 코드가 아닌 이 파일에만 둔다.
/// </summary>
public sealed class AppSettings
{
    public string ServerUrl { get; set; } = "ws://localhost:4000";
    public bool ShowBalloon { get; set; } = true;   // 채팅·상태 변화 알림 풍선
    public bool PlaySound { get; set; } = false;    // 알림 풍선과 함께 소리

    static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VishasController");
    static readonly string FilePath = Path.Combine(Dir, "settings.json");
    static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new();
        }
        catch { /* 깨진 파일이면 기본값 */ }
        return new();
    }

    public void Save()
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json));
    }

    /// <summary>ws:// 또는 wss:// 주소인지 확인한다.</summary>
    public static bool TryParseServerUrl(string text, out Uri uri)
    {
        if (Uri.TryCreate(text.Trim(), UriKind.Absolute, out var u) && (u.Scheme == "ws" || u.Scheme == "wss"))
        {
            uri = u;
            return true;
        }
        uri = null!;
        return false;
    }
}

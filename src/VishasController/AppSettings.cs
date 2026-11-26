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

    // 모듈 ⚙️ 설정(명세 02 §5.2). 옛 파일에 없으면 기본값으로 남는다.
    public ModuleSettings Synapse { get; set; } = new();
    public ModuleSettings Body { get; set; } = new();
    public ModuleSettings Face { get; set; } = new();

    static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VishasController");
    static readonly string FilePath = Path.Combine(Dir, "settings.json");
    static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new();
                // 파일에 null로 적혀 있으면 기본값으로
                s.Synapse ??= new();
                s.Body ??= new();
                s.Face ??= new();
                return s;
            }
        }
        catch { /* 깨진 파일이면 기본값 */ }
        return new();
    }

    /// <summary>⚙️ 설정이 있는 모듈(시냅스·바디·페이스)의 설정.</summary>
    public ModuleSettings ModuleOf(Module module) => module switch
    {
        Module.Synapse => Synapse,
        Module.Body => Body,
        Module.Face => Face,
        _ => throw new ArgumentOutOfRangeException(nameof(module)),
    };

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

/// <summary>모듈 하나의 ⚙️ 설정.</summary>
public sealed class ModuleSettings
{
    public string Path { get; set; } = "";             // ⏻로 실행할 프로그램 경로
    public bool AutoActivate { get; set; } = false;    // ⏻로 실행하면 ▶️ 기능도 켜기 (바디·페이스)
}

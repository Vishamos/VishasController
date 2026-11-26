using System.Text.Json;

namespace VishasController;

/// <summary>
/// 사용자 설정. %LOCALAPPDATA%\VishaSTD\controller\settings.json 에 저장한다(명세 02 §5.1).
/// 서버 주소(LAN IP)는 공개 저장소에 올리지 않도록 코드가 아닌 이 파일에만 둔다.
/// </summary>
public sealed class AppSettings
{
    public string ServerUrl { get; set; } = "ws://localhost:4000";
    public bool ShowBalloon { get; set; } = true;        // 채팅 알림 풍선 (트레이 "알림")
    public bool ShowStatusAlerts { get; set; } = true;   // 서버·치지직 상태 알림 풍선 (트레이 "중요 경고")
    public bool PlaySound { get; set; } = false;         // 알림 풍선과 함께 소리

    // 모듈 ⚙️ 설정(명세 02 §5.2). 옛 파일에 없으면 기본값으로 남는다.
    public ModuleSettings Synapse { get; set; } = new();
    public ModuleSettings Body { get; set; } = new();
    public ModuleSettings Face { get; set; } = new();

    static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VishaSTD", "controller");
    static readonly string FilePath = Path.Combine(Dir, "settings.json");
    // 옛 위치(UI-5까지). 새 위치에 파일이 없을 때 한 번 복사해 온다. 옛 파일은 지우지 않는다.
    static readonly string OldFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VishasController", "settings.json");
    static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            var source = FilePath;
            if (!File.Exists(FilePath) && File.Exists(OldFilePath))
            {
                try
                {
                    Directory.CreateDirectory(Dir);
                    File.Copy(OldFilePath, FilePath, overwrite: false);
                }
                catch { source = OldFilePath; /* 복사 실패면 옛 파일을 그대로 읽는다(저장은 새 위치로) */ }
            }
            if (File.Exists(source))
            {
                var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(source)) ?? new();
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
    public bool AutoRun { get; set; } = false;         // 자동 실행: 시냅스는 컨트롤러 시작 시, 바디·페이스는 시냅스 등록 시. 저장만 한다(실행은 Proc-1)
}

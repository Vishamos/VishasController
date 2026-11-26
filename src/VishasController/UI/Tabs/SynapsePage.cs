namespace VishasController.UI.Tabs;

/// <summary>
/// 시냅스 탭: 하위 탭 3개(상태, 프리셋, 단축키). 하위 탭에는 점이 없다.
/// </summary>
public sealed class SynapsePage : UserControl
{
    readonly ModuleStatusTab _status = new();

    /// <summary>상태 하위 탭 모듈 줄의 ⚙️를 눌렀을 때</summary>
    public event Action<Module>? SettingsClicked;

    public SynapsePage()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        AddPage(tabs, "상태", _status);
        AddPage(tabs, "프리셋", new PresetTab());
        AddPage(tabs, "단축키", new HotkeyTab());
        Controls.Add(tabs);

        _status.SettingsClicked += m => SettingsClicked?.Invoke(m);
    }

    static void AddPage(TabControl tabs, string title, UserControl content)
    {
        content.Dock = DockStyle.Fill;
        var page = new TabPage(title) { UseVisualStyleBackColor = true };
        page.Controls.Add(content);
        tabs.TabPages.Add(page);
    }

    /// <summary>상태 하위 탭의 시냅스 줄과 바디·페이스 줄 표시를 바꾼다.</summary>
    public void SetSynapse(Health health, string text, bool registered) => _status.SetSynapse(health, text, registered);
}

namespace VishasController.UI.Tabs;

/// <summary>
/// 시냅스 › 상태 하위 탭: 모듈 줄 3개(시냅스 ⏻ ⚙️ / 바디·페이스 ⏻ ▶️ ⚙️).
/// 바디·페이스는 연결 상태를 받을 방법이 생길 때까지 준비 중(⚫)이다.
/// </summary>
public sealed class ModuleStatusTab : UserControl
{
    readonly ModuleRow _synapse;

    /// <summary>모듈 줄의 ⚙️를 눌렀을 때</summary>
    public event Action<Module>? SettingsClicked;

    public ModuleStatusTab()
    {
        Padding = new Padding(6);
        _synapse = AddRow(Module.Synapse, hasToggle: false, TrayIcons.ColorOf(Health.Idle));
        _synapse.SetState(Health.Idle, "-");
        AddRow(Module.Body, hasToggle: true, TrayIcons.PendingColor);
        AddRow(Module.Face, hasToggle: true, TrayIcons.PendingColor);
    }

    ModuleRow AddRow(Module module, bool hasToggle, Color dot)
    {
        var row = new ModuleRow(module, hasToggle, dot) { Dock = DockStyle.Top };
        row.SettingsClicked += () => SettingsClicked?.Invoke(module);
        Controls.Add(row);
        row.BringToFront();   // Dock=Top은 나중에 넣은 것이 위로 가므로 순서를 맞춘다
        return row;
    }

    /// <summary>시냅스 줄의 점과 상태 글자를 바꾼다.</summary>
    public void SetSynapse(Health health, string text) => _synapse.SetState(health, text);
}

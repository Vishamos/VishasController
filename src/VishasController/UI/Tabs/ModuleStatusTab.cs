namespace VishasController.UI.Tabs;

/// <summary>
/// 시냅스 › 상태 하위 탭: 모듈 줄 3개(시냅스 ⏻ ⚙️ / 바디·페이스 ⏻ ▶️ ⚙️).
/// 바디·페이스는 연결 상태를 받을 방법이 생길 때까지 준비 중(⚫)이고,
/// 시냅스가 등록됐을 때만 시냅스 줄 아래에 들여 써서 보인다(명세 02 §3.1).
/// </summary>
public sealed class ModuleStatusTab : UserControl
{
    const int ChildIndent = 24;   // 바디·페이스 줄 들여쓰기(px)

    readonly ModuleRow _synapse;
    readonly ModuleRow _body;
    readonly ModuleRow _face;

    /// <summary>모듈 줄의 ⚙️를 눌렀을 때</summary>
    public event Action<Module>? SettingsClicked;

    public ModuleStatusTab()
    {
        Padding = new Padding(6);
        _synapse = AddRow(Module.Synapse, hasToggle: false, TrayIcons.ColorOf(Health.Idle));
        _synapse.SetState(Health.Idle, "-");
        _body = AddRow(Module.Body, hasToggle: true, TrayIcons.PendingColor);
        _face = AddRow(Module.Face, hasToggle: true, TrayIcons.PendingColor);
        foreach (var row in new[] { _body, _face })
        {
            row.Padding = new Padding(ChildIndent, 0, 0, 0);   // Dock 배치는 Margin을 무시하므로 Padding으로 들여 쓴다
            row.Visible = false;
        }
    }

    ModuleRow AddRow(Module module, bool hasToggle, Color dot)
    {
        var row = new ModuleRow(module, hasToggle, dot) { Dock = DockStyle.Top };
        row.SettingsClicked += () => SettingsClicked?.Invoke(module);
        Controls.Add(row);
        row.BringToFront();   // Dock=Top은 나중에 넣은 것이 위로 가므로 순서를 맞춘다
        return row;
    }

    /// <summary>시냅스 줄의 점과 상태 글자를 바꾸고, 등록 여부에 따라 바디·페이스 줄을 보이거나 숨긴다.</summary>
    public void SetSynapse(Health health, string text, bool registered)
    {
        _synapse.SetState(health, text);
        _body.Visible = registered;
        _face.Visible = registered;
    }
}

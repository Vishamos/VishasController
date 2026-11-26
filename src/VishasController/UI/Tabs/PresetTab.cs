namespace VishasController.UI.Tabs;

/// <summary>
/// 시냅스 › 프리셋 하위 탭: 프리셋 목록(비어 있음)과 "+ 프리셋 추가"(비활성).
/// </summary>
public sealed class PresetTab : UserControl
{
    public PresetTab()
    {
        var list = new ListBox { Dock = DockStyle.Fill, Enabled = false };
        var add = new Button { Text = "+ 프리셋 추가", Dock = DockStyle.Bottom, AutoSize = true, Enabled = false };

        Controls.Add(list);
        Controls.Add(add);
    }
}

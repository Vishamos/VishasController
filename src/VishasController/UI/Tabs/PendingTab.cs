namespace VishasController.UI.Tabs;

/// <summary>
/// 페이스·바디 탭: 연결 상태 자리(준비 중).
/// </summary>
public sealed class PendingTab : UserControl
{
    public PendingTab(string name)
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(3, 6, 3, 3),
        };
        panel.Controls.Add(new Label { Text = $"준비 중: {name} 연결 기능은 아직 없습니다.", AutoSize = true, Margin = new Padding(3, 3, 3, 12) });
        panel.Controls.Add(new Label { Text = "연결 상태: -", AutoSize = true, Enabled = false });
        Controls.Add(panel);
    }
}

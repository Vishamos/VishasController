namespace VishasController.UI.Tabs;

/// <summary>
/// 종합 탭: 프로그램별 상태 표와 프리셋 자리(비활성).
/// </summary>
public sealed class OverviewTab : UserControl
{
    readonly Dictionary<string, (Label Dot, Label Text)> _rows = new();

    public OverviewTab()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 3,
            Padding = new Padding(6),
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        foreach (var name in new[] { "시냅스", "치지직", "OBS", "페이스", "바디", "비샤모스" })
        {
            var nameLabel = new Label { Text = name, AutoSize = true, Margin = new Padding(3, 3, 12, 3) };
            // ●는 이모지가 아닌 도형 글자라 ForeColor 색으로 그려진다
            var dot = new Label { Text = "●", AutoSize = true, ForeColor = TrayIcons.ColorOf(Health.Idle) };
            var text = new Label { Text = "준비 중", AutoSize = true };
            table.Controls.Add(nameLabel);
            table.Controls.Add(dot);
            table.Controls.Add(text);
            _rows[name] = (dot, text);
        }

        var status = new GroupBox { Text = "상태", Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        status.Controls.Add(table);

        var presetNote = new Label
        {
            Text = "OBS 장면과 비샤모스 월드·캐릭터를 묶어 한 번에 바꿉니다(준비 중)",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(3, 3, 3, 6),
        };
        var presetList = new ListBox { Dock = DockStyle.Fill, Enabled = false };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true };
        foreach (var label in new[] { "적용", "새로 만들기", "편집", "삭제" })
            buttons.Controls.Add(new Button { Text = label, AutoSize = true, Enabled = false });

        var preset = new GroupBox { Text = "프리셋", Dock = DockStyle.Fill };
        preset.Controls.Add(presetList);
        preset.Controls.Add(buttons);
        preset.Controls.Add(presetNote);

        Controls.Add(preset);
        Controls.Add(status);
    }

    /// <summary>상태 표의 한 줄을 바꾼다.</summary>
    public void SetRow(string name, Health health, string text)
    {
        var (dot, label) = _rows[name];
        dot.ForeColor = TrayIcons.ColorOf(health);
        label.Text = text;
    }
}

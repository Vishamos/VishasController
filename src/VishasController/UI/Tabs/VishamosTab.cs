namespace VishasController.UI.Tabs;

/// <summary>
/// 비샤모스 탭: 장면 선택·캐릭터 배치 자리(비활성, 준비 중).
/// </summary>
public sealed class VishamosTab : UserControl
{
    public VishamosTab()
    {
        var note = new Label
        {
            Text = "준비 중: 비샤모스 연결 기능은 아직 없습니다.",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(3, 6, 3, 6),
        };

        var scenes = new GroupBox { Text = "장면 [Vishamos-Space]", Dock = DockStyle.Fill };
        scenes.Controls.Add(new ListBox { Dock = DockStyle.Fill, Enabled = false });
        var sceneButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true };
        sceneButtons.Controls.Add(new Button { Text = "선택", AutoSize = true, Enabled = false });
        scenes.Controls.Add(sceneButtons);

        var characters = new GroupBox { Text = "캐릭터 배치", Dock = DockStyle.Fill, Enabled = false };

        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.Controls.Add(scenes, 0, 0);
        grid.Controls.Add(characters, 1, 0);

        Controls.Add(grid);
        Controls.Add(note);
    }
}

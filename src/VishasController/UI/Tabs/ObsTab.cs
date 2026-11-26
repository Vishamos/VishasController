namespace VishasController.UI.Tabs;

/// <summary>
/// OBS 탭: 장면 전환·마이크·녹음 자리(비활성, 준비 중).
/// </summary>
public sealed class ObsTab : UserControl
{
    public ObsTab()
    {
        var note = new Label
        {
            Text = "준비 중: OBS 연결 기능은 아직 없습니다.",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(3, 6, 3, 6),
        };

        var scenes = new GroupBox { Text = "장면", Dock = DockStyle.Fill };
        scenes.Controls.Add(new ListBox { Dock = DockStyle.Fill, Enabled = false });
        var sceneButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true };
        sceneButtons.Controls.Add(new Button { Text = "전환", AutoSize = true, Enabled = false });
        scenes.Controls.Add(sceneButtons);

        var controls = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true };
        controls.Controls.Add(new CheckBox { Text = "마이크 음소거", Appearance = Appearance.Button, AutoSize = true, Enabled = false });
        controls.Controls.Add(new Button { Text = "녹음 시작", AutoSize = true, Enabled = false });
        controls.Controls.Add(new Label { Text = "방송 상태: -", AutoSize = true, Enabled = false, Margin = new Padding(12, 8, 3, 3) });

        Controls.Add(scenes);
        Controls.Add(controls);
        Controls.Add(note);
    }
}

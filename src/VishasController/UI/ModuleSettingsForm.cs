namespace VishasController.UI;

/// <summary>
/// 모듈 ⚙️ 설정 창(모달). 모든 모듈: 실행 경로. 바디·페이스: 자동 켜기. 시냅스: 서버 주소.
/// </summary>
public sealed class ModuleSettingsForm : Form
{
    readonly TextBox _path;
    readonly CheckBox? _autoActivate;
    readonly TextBox? _url;

    public string ExecutablePath => _path.Text.Trim();
    public bool AutoActivate => _autoActivate?.Checked ?? false;
    /// <summary>시냅스 창에서만 쓴다. 다른 모듈은 빈 문자열.</summary>
    public string ServerUrl => _url?.Text.Trim() ?? "";

    /// <param name="serverUrl">시냅스면 지금 서버 주소, 다른 모듈은 null(입력칸 없음)</param>
    public ModuleSettingsForm(Module module, ModuleSettings current, string? serverUrl)
    {
        Text = $"{module.DisplayName()} 설정";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ShowInTaskbar = false;

        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Padding = new Padding(9),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        layout.Controls.Add(new Label { Text = "실행 경로", AutoSize = true, Margin = new Padding(3, 6, 3, 3) });
        layout.SetColumnSpan(layout.Controls[^1], 2);
        _path = new TextBox { Text = current.Path, Dock = DockStyle.Fill };
        var browse = new Button { Text = "찾아보기…", AutoSize = true };
        browse.Click += (_, _) => Browse();
        layout.Controls.Add(_path);
        layout.Controls.Add(browse);

        if (module is Module.Body or Module.Face)
        {
            _autoActivate = new CheckBox { Text = "⏻로 실행하면 ▶️ 기능도 켜기", Checked = current.AutoActivate, AutoSize = true, Margin = new Padding(3, 9, 3, 3) };
            layout.Controls.Add(_autoActivate);
            layout.SetColumnSpan(_autoActivate, 2);
        }

        if (serverUrl != null)
        {
            var label = new Label { Text = "서버 주소 (예: ws://<서버 PC IP>:4000)", AutoSize = true, Margin = new Padding(3, 12, 3, 3) };
            layout.Controls.Add(label);
            layout.SetColumnSpan(label, 2);
            _url = new TextBox { Text = serverUrl, Dock = DockStyle.Fill };
            layout.Controls.Add(_url);
            layout.SetColumnSpan(_url, 2);
        }

        var ok = new Button { Text = "확인", Width = 75, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "취소", Width = 75, DialogResult = DialogResult.Cancel };
        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 12, 0, 0),
        };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        layout.Controls.Add(buttons);
        layout.SetColumnSpan(buttons, 2);

        Controls.Add(layout);
        AcceptButton = ok;
        CancelButton = cancel;

        FormClosing += (sender, e) =>
        {
            if (DialogResult == DialogResult.OK && _url != null && !AppSettings.TryParseServerUrl(ServerUrl, out _))
            {
                MessageBox.Show(this, "ws:// 또는 wss:// 로 시작하는 주소를 입력하세요.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
        };
    }

    void Browse()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "프로그램 (*.exe)|*.exe|모든 파일 (*.*)|*.*",
            CheckFileExists = true,
        };
        if (ExecutablePath.Length > 0)
        {
            // 입력칸이 잘못된 경로여도 대화상자는 열려야 하므로 예외를 삼킨다
            try { dlg.InitialDirectory = Path.GetDirectoryName(ExecutablePath) ?? ""; }
            catch { /* 경로 형식 오류 */ }
        }
        if (dlg.ShowDialog(this) == DialogResult.OK) _path.Text = dlg.FileName;
    }
}

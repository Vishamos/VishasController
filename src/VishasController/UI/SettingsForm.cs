namespace VishasController.UI;

/// <summary>서버 주소 설정 대화상자.</summary>
public sealed class SettingsForm : Form
{
    readonly TextBox _url;

    public string ServerUrl => _url.Text.Trim();

    public SettingsForm(string currentUrl)
    {
        Text = "설정";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(420, 130);

        var label = new Label
        {
            Text = "서버 주소 (예: ws://192.168.0.10:4000)",
            Location = new Point(12, 14),
            AutoSize = true,
        };
        _url = new TextBox
        {
            Text = currentUrl,
            Location = new Point(12, 38),
            Width = 396,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
        };
        var ok = new Button { Text = "저장", Location = new Point(252, 86), Width = 75, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "취소", Location = new Point(333, 86), Width = 75, DialogResult = DialogResult.Cancel };

        Controls.AddRange(new Control[] { label, _url, ok, cancel });
        AcceptButton = ok;
        CancelButton = cancel;

        FormClosing += (sender, e) =>
        {
            if (DialogResult == DialogResult.OK && !AppSettings.TryParseServerUrl(ServerUrl, out _))
            {
                MessageBox.Show(this, "ws:// 또는 wss:// 로 시작하는 주소를 입력하세요.", "설정",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
        };
    }
}

namespace VishasController.UI.Tabs;

/// <summary>
/// 시냅스 탭: 서버 주소(호스트 가림), 연결 상태, 연결 번호, 다시 접속·주소 설정 버튼.
/// </summary>
public sealed class SynapseTab : UserControl
{
    readonly Label _address;
    readonly Label _status;
    readonly Label _connection;

    /// <summary>"다시 접속"을 눌렀을 때 (트레이 메뉴와 같은 동작)</summary>
    public event Action? ReconnectClicked;

    /// <summary>"서버 주소 설정…"을 눌렀을 때 (트레이 메뉴와 같은 동작)</summary>
    public event Action? SettingsClicked;

    public SynapseTab()
    {
        _address = new Label { Text = "서버 주소: -", AutoSize = true };
        _status = new Label { Text = "상태: -", AutoSize = true };
        _connection = new Label { Text = "연결 번호 -", AutoSize = true, Margin = new Padding(3, 3, 3, 12) };

        var reconnect = new Button { Text = "다시 접속", AutoSize = true };
        reconnect.Click += (_, _) => ReconnectClicked?.Invoke();
        var settings = new Button { Text = "서버 주소 설정…", AutoSize = true };
        settings.Click += (_, _) => SettingsClicked?.Invoke();
        var buttons = new FlowLayoutPanel { AutoSize = true, Margin = Padding.Empty };
        buttons.Controls.Add(reconnect);
        buttons.Controls.Add(settings);

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(3, 6, 3, 3),
        };
        panel.Controls.AddRange(new Control[] { _address, _status, _connection, buttons });
        Controls.Add(panel);
    }

    /// <param name="serverUrl">설정의 서버 주소. 방송 화면에 잡힐 수 있으므로 호스트를 가려 표시한다.</param>
    /// <param name="connection">"연결 번호 N" 또는 "연결 번호 -"</param>
    public void SetState(string serverUrl, string status, Color statusColor, string connection)
    {
        _address.Text = "서버 주소: " + MaskHost(serverUrl);
        _status.Text = "상태: " + status;
        _status.ForeColor = statusColor;
        _connection.Text = connection;
    }

    /// <summary>ws://***:4000 처럼 스킴과 포트만 남긴다. 경로도 보이지 않게 뺀다.</summary>
    static string MaskHost(string serverUrl)
    {
        if (!AppSettings.TryParseServerUrl(serverUrl, out var uri)) return "(주소 형식 오류)";
        return uri.IsDefaultPort ? $"{uri.Scheme}://***" : $"{uri.Scheme}://***:{uri.Port}";
    }
}

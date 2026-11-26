namespace VishasController.UI.Tabs;

/// <summary>
/// 종합 탭: 프로그램 표(줄마다 이름·색 점·상태 글자·줄별 조작)와 프리셋 자리(비활성).
/// </summary>
public sealed class OverviewTab : UserControl
{
    // 연결 기능이 있는 줄은 시냅스·치지직뿐이고, 나머지는 준비 중(⚫)이다
    static readonly string[] RowNames = { "시냅스", "페이스", "바디", "OBS", "치지직", "비샤모스" };
    static readonly HashSet<string> LiveRows = new() { "시냅스", "치지직" };
    static readonly HashSet<string> RunnableRows = new() { "시냅스", "페이스", "바디" };

    readonly Dictionary<string, (Label Dot, Label Text)> _rows = new();
    readonly Label _connection;

    /// <summary>시냅스 줄의 "다시 접속"을 눌렀을 때</summary>
    public event Action? ReconnectClicked;

    /// <summary>시냅스 줄의 "주소 설정…"을 눌렀을 때</summary>
    public event Action? SettingsClicked;

    public OverviewTab()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 4,
            Padding = new Padding(6),
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _connection = new Label { Text = "연결 번호 -", AutoSize = true, Margin = new Padding(3, 8, 12, 3) };

        int row = 0;
        foreach (var name in RowNames)
        {
            bool live = LiveRows.Contains(name);
            var nameLabel = new Label { Text = name, AutoSize = true, Margin = new Padding(3, 8, 12, 3) };
            // ●는 이모지가 아닌 도형 글자라 ForeColor 색으로 그려진다
            var dot = new Label
            {
                Text = "●",
                AutoSize = true,
                Margin = new Padding(3, 8, 3, 3),
                ForeColor = live ? TrayIcons.ColorOf(Health.Idle) : TrayIcons.PendingColor,
            };
            var text = new Label { Text = live ? "-" : "", AutoSize = true, Margin = new Padding(3, 8, 12, 3) };

            var actions = new FlowLayoutPanel { AutoSize = true, Margin = Padding.Empty, WrapContents = false };
            if (RunnableRows.Contains(name))
            {
                actions.Controls.Add(new CheckBox { Text = "On/Off", Appearance = Appearance.Button, AutoSize = true, Enabled = false });
                actions.Controls.Add(new Button { Text = "실행", AutoSize = true, Enabled = false });
            }

            table.Controls.Add(nameLabel, 0, row);
            table.Controls.Add(dot, 1, row);
            table.Controls.Add(text, 2, row);
            table.Controls.Add(actions, 3, row);
            _rows[name] = (dot, text);
            row++;

            if (name == "시냅스")
            {
                var detail = CreateSynapseDetail();
                table.Controls.Add(detail, 1, row);
                table.SetColumnSpan(detail, 3);
                row++;
            }
        }

        var status = new GroupBox { Text = "상태", Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        status.Controls.Add(table);

        var presetNote = new Label
        {
            Text = "OBS 장면과 비샤모스 월드·캐릭터를 묶어 한 번에 바꿉니다",
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

    /// <summary>시냅스 줄 바로 아래 칸: 연결 번호, 다시 접속·주소 설정 버튼. 서버 주소는 방송 화면에 잡힐 수 있어 표시하지 않는다.</summary>
    Control CreateSynapseDetail()
    {
        var reconnect = new Button { Text = "다시 접속", AutoSize = true };
        reconnect.Click += (_, _) => ReconnectClicked?.Invoke();
        var settings = new Button { Text = "주소 설정…", AutoSize = true };
        settings.Click += (_, _) => SettingsClicked?.Invoke();

        var detail = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 0, 0, 6), WrapContents = false };
        detail.Controls.AddRange(new Control[] { _connection, reconnect, settings });
        return detail;
    }

    /// <summary>시냅스 줄(점, 상태, 연결 번호)을 바꾼다.</summary>
    /// <param name="connection">"연결 번호 N" 또는 "연결 번호 -"</param>
    public void SetSynapse(Health health, string status, string connection)
    {
        SetRow("시냅스", health, status);
        _connection.Text = connection;
    }

    /// <summary>연결 기능이 있는 줄(시냅스·치지직)의 점과 상태 글자를 바꾼다.</summary>
    public void SetRow(string name, Health health, string text)
    {
        var (dot, label) = _rows[name];
        dot.ForeColor = TrayIcons.ColorOf(health);
        label.Text = text;
    }
}

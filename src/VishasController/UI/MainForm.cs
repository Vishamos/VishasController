using System.Drawing.Drawing2D;
using VishasController.Net;
using VishasController.UI.Tabs;

namespace VishasController.UI;

/// <summary>
/// 메인 창: 탭 4개와 아래 상태 표시줄. 닫기 버튼을 눌러도 종료하지 않고 트레이로 숨는다.
/// </summary>
public sealed class MainForm : Form
{
    const string ChzzkTitle = "치지직";

    readonly TabControl _tabs;
    readonly TabPage _chzzkPage;
    readonly Dictionary<TabPage, Color> _dots = new();   // 없는 탭(종합)은 점을 그리지 않는다
    readonly OverviewTab _overview = new();
    readonly ChatTab _chat = new();
    readonly ToolStripStatusLabel _serverLabel;
    readonly ToolStripStatusLabel _chzzkLabel;
    readonly ToolStripStatusLabel _countLabel;

    /// <summary>사용자가 치지직 탭을 보고 있게 됐을 때 (안 읽음 표시를 지우는 데 쓴다)</summary>
    public event Action? Viewed;

    /// <summary>종합 탭 시냅스 줄에서 "다시 접속"을 눌렀을 때</summary>
    public event Action? ReconnectRequested;

    /// <summary>종합 탭 시냅스 줄에서 "주소 설정…"을 눌렀을 때</summary>
    public event Action? SettingsRequested;

    public MainForm()
    {
        Text = "VishasController";
        Size = new Size(720, 480);
        MinimumSize = new Size(420, 240);
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;

        // 탭 제목 앞 색 점은 이모지가 흑백으로 나올 수 있어 직접 그린다. Padding.X로 점 자리를 넓힌다.
        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            DrawMode = TabDrawMode.OwnerDrawFixed,
            Padding = new Point(16, 4),
        };
        _tabs.DrawItem += DrawTab;
        _tabs.SelectedIndexChanged += (_, _) => { if (IsBeingWatched) Viewed?.Invoke(); };

        AddPage("종합", _overview, dot: null);
        AddPage("OBS", new ObsTab(), dot: TrayIcons.PendingColor);
        _chzzkPage = AddPage(ChzzkTitle, _chat, dot: TrayIcons.ColorOf(Health.Idle));
        AddPage("비샤모스", new VishamosTab(), dot: TrayIcons.PendingColor);

        _chat.CountChanged += UpdateCount;
        _overview.ReconnectClicked += () => ReconnectRequested?.Invoke();
        _overview.SettingsClicked += () => SettingsRequested?.Invoke();

        var status = new StatusStrip();
        _serverLabel = new ToolStripStatusLabel("서버: -");
        _chzzkLabel = new ToolStripStatusLabel("치지직: -") { Margin = new Padding(16, 3, 0, 2) };
        _countLabel = new ToolStripStatusLabel("") { Spring = true, TextAlign = ContentAlignment.MiddleRight };
        status.Items.AddRange(new ToolStripItem[] { _serverLabel, _chzzkLabel, _countLabel });

        Controls.Add(_tabs);
        Controls.Add(status);

        Activated += (_, _) => { if (_tabs.SelectedTab == _chzzkPage) Viewed?.Invoke(); };
    }

    /// <summary>창이 보이고 포커스가 있고 치지직 탭이 선택됐는지 (이때는 알림 풍선을 띄우지 않는다)</summary>
    public bool IsBeingWatched =>
        Visible && WindowState != FormWindowState.Minimized && ContainsFocus && _tabs.SelectedTab == _chzzkPage;

    /// <summary>마지막에 보던 탭으로 창을 연다.</summary>
    public void ShowAndActivate()
    {
        if (!Visible) Show();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
        if (_tabs.SelectedTab == _chzzkPage) Viewed?.Invoke();
    }

    /// <summary>치지직 탭으로 창을 연다 (채팅 알림 풍선 클릭).</summary>
    public void ShowChzzkTab()
    {
        _tabs.SelectedTab = _chzzkPage;
        ShowAndActivate();
    }

    public void AddChat(ChzzkChat chat) => _chat.AddChat(chat);

    public void SetServerStatus(string text, Color color)
    {
        _serverLabel.Text = "서버: " + text;
        _serverLabel.ForeColor = color;
    }

    public void SetChzzkStatus(string text, Color color)
    {
        _chzzkLabel.Text = "치지직: " + text;
        _chzzkLabel.ForeColor = color;
    }

    /// <summary>종합 탭의 시냅스 줄을 바꾼다.</summary>
    public void SetSynapse(Health health, string status, string connection) =>
        _overview.SetSynapse(health, status, connection);

    /// <summary>치지직 탭 점과 종합 탭의 치지직 줄을 바꾼다.</summary>
    public void SetChzzk(Health health, string status)
    {
        SetDot(_chzzkPage, health);
        _overview.SetRow("치지직", health, status);
    }

    /// <summary>치지직 탭 제목의 안 읽은 수. 0이면 숫자를 뺀다.</summary>
    public void SetUnread(int count)
    {
        var title = count > 0 ? $"{ChzzkTitle} ({count})" : ChzzkTitle;
        if (_chzzkPage.Text != title) _chzzkPage.Text = title;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // X 버튼은 숨기기만 한다. 종료는 트레이 메뉴에서.
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnFormClosing(e);
    }

    /// <param name="dot">탭 제목 앞 점 색. null이면 점을 그리지 않는다.</param>
    TabPage AddPage(string title, UserControl content, Color? dot)
    {
        content.Dock = DockStyle.Fill;
        var page = new TabPage(title) { UseVisualStyleBackColor = true };
        page.Controls.Add(content);
        _tabs.TabPages.Add(page);
        if (dot is Color color) _dots[page] = color;
        return page;
    }

    void SetDot(TabPage page, Health health)
    {
        var color = TrayIcons.ColorOf(health);
        if (_dots[page] == color) return;
        _dots[page] = color;
        _tabs.Invalidate();
    }

    void DrawTab(object? sender, DrawItemEventArgs e)
    {
        var page = _tabs.TabPages[e.Index];
        var bounds = e.Bounds;
        var g = e.Graphics;
        using (var bg = new SolidBrush(SystemColors.Control)) g.FillRectangle(bg, bounds);

        int textLeft = bounds.Left;
        if (_dots.TryGetValue(page, out var color))
        {
            int d = Math.Max(6, _tabs.Font.Height / 2);
            var dot = new Rectangle(bounds.Left + 6, bounds.Top + (bounds.Height - d) / 2, d, d);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var b = new SolidBrush(color);
            g.FillEllipse(b, dot);
            textLeft = dot.Right + 2;
        }

        var textBounds = Rectangle.FromLTRB(textLeft, bounds.Top, bounds.Right, bounds.Bottom);
        TextRenderer.DrawText(g, page.Text, _tabs.Font, textBounds, SystemColors.ControlText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }

    void UpdateCount() => _countLabel.Text = $"표시 {_chat.Displayed} · 받은 채팅 {_chat.Total}";
}

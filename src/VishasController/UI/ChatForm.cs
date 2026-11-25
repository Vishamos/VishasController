using VishasController.Net;

namespace VishasController.UI;

/// <summary>
/// 최근 채팅 목록 창. 닫기 버튼을 눌러도 종료하지 않고 트레이로 숨는다.
/// </summary>
public sealed class ChatForm : Form
{
    const int MaxRows = 1000;

    readonly ListView _list;
    readonly ToolStripStatusLabel _serverLabel;
    readonly ToolStripStatusLabel _chzzkLabel;
    readonly ToolStripStatusLabel _countLabel;
    int _total;

    /// <summary>사용자가 창을 보고 있게 됐을 때 (안 읽음 표시를 지우는 데 쓴다)</summary>
    public event Action? Viewed;

    public ChatForm()
    {
        Text = "VishasController · 치지직 채팅";
        Size = new Size(720, 480);
        MinimumSize = new Size(420, 240);
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;

        _list = new BufferedListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            ShowItemToolTips = true,
            HideSelection = false,
        };
        _list.Columns.Add("시각", 70);
        _list.Columns.Add("역할", 80);
        _list.Columns.Add("닉네임", 130);
        _list.Columns.Add("내용", 400);
        _list.Resize += (_, _) => FitLastColumn();

        var copy = new ContextMenuStrip();
        copy.Items.Add("내용 복사", null, (_, _) => CopySelected());
        copy.Items.Add("목록 지우기", null, (_, _) => { _list.Items.Clear(); UpdateCount(); });
        _list.ContextMenuStrip = copy;

        var status = new StatusStrip();
        _serverLabel = new ToolStripStatusLabel("서버: -");
        _chzzkLabel = new ToolStripStatusLabel("치지직: -") { Margin = new Padding(16, 3, 0, 2) };
        _countLabel = new ToolStripStatusLabel("") { Spring = true, TextAlign = ContentAlignment.MiddleRight };
        status.Items.AddRange(new ToolStripItem[] { _serverLabel, _chzzkLabel, _countLabel });

        Controls.Add(_list);
        Controls.Add(status);

        Activated += (_, _) => Viewed?.Invoke();
    }

    /// <summary>창이 보이고 포커스가 있는지 (이때는 알림 풍선을 띄우지 않는다)</summary>
    public bool IsBeingWatched => Visible && WindowState != FormWindowState.Minimized && ContainsFocus;

    public void ShowAndActivate()
    {
        if (!Visible) Show();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
        Viewed?.Invoke();
    }

    public void AddChat(ChzzkChat chat)
    {
        bool atBottom = IsScrolledToBottom();

        var item = new ListViewItem(chat.Time.ToString("HH:mm:ss"));
        item.SubItems.Add(chat.RoleLabel);
        item.SubItems.Add(chat.Nickname);
        item.SubItems.Add(chat.DisplayContent);
        item.ToolTipText = $"{chat.Nickname}: {chat.DisplayContent}";
        item.ForeColor = chat.Role switch
        {
            "streamer" => Color.FromArgb(140, 60, 200),
            "streaming_channel_manager" or "streaming_chat_manager" => Color.FromArgb(20, 130, 60),
            _ => SystemColors.WindowText,
        };

        _list.BeginUpdate();
        _list.Items.Add(item);
        while (_list.Items.Count > MaxRows) _list.Items.RemoveAt(0);
        _list.EndUpdate();
        _total++;
        UpdateCount();

        // 사용자가 위로 스크롤해 옛 채팅을 보고 있으면 끌어내리지 않는다
        if (atBottom) item.EnsureVisible();
    }

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

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        FitLastColumn();
        if (_list.Items.Count > 0) _list.Items[^1].EnsureVisible();
    }

    bool IsScrolledToBottom()
    {
        if (_list.Items.Count == 0 || !_list.IsHandleCreated || !_list.Visible) return true;
        var last = _list.Items[^1];
        return _list.ClientRectangle.IntersectsWith(last.Bounds);
    }

    void FitLastColumn()
    {
        if (_list.Columns.Count == 0) return;
        int used = 0;
        for (int i = 0; i < _list.Columns.Count - 1; i++) used += _list.Columns[i].Width;
        _list.Columns[^1].Width = Math.Max(120, _list.ClientSize.Width - used);
    }

    void CopySelected()
    {
        var lines = _list.SelectedItems.Cast<ListViewItem>()
            .Select(i => $"[{i.Text}] {i.SubItems[2].Text}: {i.SubItems[3].Text}");
        var text = string.Join(Environment.NewLine, lines);
        if (text.Length > 0) Clipboard.SetText(text);
    }

    void UpdateCount() => _countLabel.Text = $"표시 {_list.Items.Count} · 받은 채팅 {_total}";

    /// <summary>깜빡임 줄인 ListView</summary>
    sealed class BufferedListView : ListView
    {
        public BufferedListView() => DoubleBuffered = true;
    }
}

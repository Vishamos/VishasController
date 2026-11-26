using VishasController.Net;

namespace VishasController.UI.Tabs;

/// <summary>
/// 치지직 탭: 최근 채팅 목록.
/// </summary>
public sealed class ChatTab : UserControl
{
    const int MaxRows = 1000;

    readonly ListView _list;
    int _total;

    /// <summary>표시 중인 채팅 수나 받은 채팅 수가 바뀌었을 때</summary>
    public event Action? CountChanged;

    /// <summary>목록에 표시 중인 채팅 수</summary>
    public int Displayed => _list.Items.Count;

    /// <summary>실행 뒤 받은 채팅 수</summary>
    public int Total => _total;

    public ChatTab()
    {
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
        copy.Items.Add("목록 지우기", null, (_, _) => { _list.Items.Clear(); CountChanged?.Invoke(); });
        _list.ContextMenuStrip = copy;

        Controls.Add(_list);
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
        CountChanged?.Invoke();

        // 사용자가 위로 스크롤해 옛 채팅을 보고 있으면 끌어내리지 않는다
        if (atBottom) item.EnsureVisible();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        // 창이 처음 보이거나 이 탭으로 돌아왔을 때 (예전 채팅 창의 OnShown과 같은 처리)
        if (!Visible) return;
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

    /// <summary>깜빡임 줄인 ListView</summary>
    sealed class BufferedListView : ListView
    {
        public BufferedListView() => DoubleBuffered = true;
    }
}

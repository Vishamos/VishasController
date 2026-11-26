namespace VishasController.UI.Tabs;

/// <summary>
/// 로그 탭: 필터 드롭다운과 목록(시각, 모듈, 내용). 최근 MaxEntries줄만 메모리에 둔다(파일 저장은 나중).
/// 서버 주소·채팅 내용·닉네임은 넣지 않는다(부르는 쪽 책임).
/// </summary>
public sealed class LogTab : UserControl
{
    const int MaxEntries = 1000;
    const string All = "전체";
    static readonly Module[] FilterOrder = { Module.Synapse, Module.Chzzk, Module.Body, Module.Face, Module.Obs, Module.Vishamos };

    readonly record struct Entry(DateTime Time, Module Module, string Text);

    readonly Queue<Entry> _entries = new();
    readonly ComboBox _filter;
    readonly ListView _list;

    public LogTab()
    {
        _filter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140, Margin = new Padding(3, 6, 3, 3) };
        _filter.Items.Add(All);
        foreach (var m in FilterOrder) _filter.Items.Add(m.DisplayName());
        _filter.SelectedIndex = 0;
        _filter.SelectedIndexChanged += (_, _) => Rebuild();

        var top = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        top.Controls.Add(_filter);

        _list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
        };
        _list.Columns.Add("시각", 80);
        _list.Columns.Add("모듈", 80);
        _list.Columns.Add("내용", 480);

        Controls.Add(_list);
        Controls.Add(top);
    }

    /// <summary>로그 한 줄을 더한다. UI 스레드에서 부른다.</summary>
    public void Add(Module module, string text)
    {
        var entry = new Entry(DateTime.Now, module, text);
        _entries.Enqueue(entry);

        _list.BeginUpdate();
        if (_entries.Count > MaxEntries)
        {
            // 목록은 걸러진 줄을 같은 순서로 보여 주므로, 지워진 가장 오래된 줄이 보이던 것이면 목록의 첫 줄이다
            var removed = _entries.Dequeue();
            if (Matches(removed) && _list.Items.Count > 0) _list.Items.RemoveAt(0);
        }
        if (Matches(entry))
            _list.Items.Add(ToItem(entry)).EnsureVisible();
        _list.EndUpdate();
    }

    bool Matches(Entry e) =>
        _filter.SelectedIndex <= 0 || FilterOrder[_filter.SelectedIndex - 1] == e.Module;

    static ListViewItem ToItem(Entry e) =>
        new(new[] { e.Time.ToString("HH:mm:ss"), e.Module.DisplayName(), e.Text });

    void Rebuild()
    {
        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var e in _entries)
            if (Matches(e)) _list.Items.Add(ToItem(e));
        if (_list.Items.Count > 0) _list.EnsureVisible(_list.Items.Count - 1);
        _list.EndUpdate();
    }
}

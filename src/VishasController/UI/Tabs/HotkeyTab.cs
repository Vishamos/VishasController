namespace VishasController.UI.Tabs;

/// <summary>
/// 시냅스 › 단축키 하위 탭: "키 / 기능" 목록(비어 있음)과 "+ 단축키 추가"(비활성).
/// </summary>
public sealed class HotkeyTab : UserControl
{
    public HotkeyTab()
    {
        var list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            Enabled = false,
        };
        list.Columns.Add("키", 160);
        list.Columns.Add("기능", 320);
        var add = new Button { Text = "+ 단축키 추가", Dock = DockStyle.Bottom, AutoSize = true, Enabled = false };

        Controls.Add(list);
        Controls.Add(add);
    }
}

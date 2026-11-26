using System.Drawing.Text;

namespace VishasController.UI.Tabs;

/// <summary>
/// 모듈 줄 하나: 색 점, 이름, 상태 글자, 버튼(⏻ ▶️ ⚙️). 모듈마다 ▶️ 유무만 다르다.
/// ⏻·▶️는 아직 비활성이다(실제 실행은 Proc-1, 기능 켜기는 서버 제어 메시지가 필요).
/// </summary>
public sealed class ModuleRow : TableLayoutPanel
{
    // 이모지는 WinForms GDI로 제대로 안 그려져 Windows 아이콘 글꼴의 글리프를 쓴다(명세 02 §4.5)
    const string RunGlyph = "";
    const string ToggleOff = "";
    const string ToggleOn = "";
    const string SettingsGlyph = "";

    // Win11은 Segoe Fluent Icons, Win10은 Segoe MDL2 Assets. 설치 여부는 한 번만 확인한다
    static readonly Lazy<string> IconFontName = new(() =>
    {
        using var fonts = new InstalledFontCollection();
        return fonts.Families.Any(f => f.Name == "Segoe Fluent Icons") ? "Segoe Fluent Icons" : "Segoe MDL2 Assets";
    });

    readonly Label _dot;
    readonly Label _status;
    readonly Font _iconFont;
    readonly ToolTip _toolTip = new();

    /// <summary>⚙️를 눌렀을 때</summary>
    public event Action? SettingsClicked;

    public ModuleRow(Module module, bool hasToggle, Color dotColor)
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ColumnCount = 4;

        ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // ●는 이모지가 아닌 도형 글자라 ForeColor 색으로 그려진다
        _dot = new Label { Text = "●", AutoSize = true, Margin = new Padding(3, 8, 3, 3), ForeColor = dotColor };
        var name = new Label { Text = module.DisplayName(), AutoSize = true, Margin = new Padding(3, 8, 3, 3) };
        _status = new Label { Text = "", AutoSize = true, Margin = new Padding(3, 8, 12, 3) };

        // 포인트 단위라 고 DPI에서도 기본 글꼴과 같은 비율로 커진다
        _iconFont = new Font(IconFontName.Value, Control.DefaultFont.SizeInPoints);

        var buttons = new FlowLayoutPanel { AutoSize = true, Margin = Padding.Empty, WrapContents = false };
        var run = new Button { Text = RunGlyph, Font = _iconFont, Enabled = false };
        // 세 버튼은 같은 정사각형(명세 02 §4.5). 한 변은 아이콘 글꼴로 계산한 버튼 높이라 고 DPI에서도 비율이 맞는다
        var side = run.GetPreferredSize(Size.Empty).Height;
        var buttonSize = new Size(side, side);
        run.Size = buttonSize;
        _toolTip.SetToolTip(run, "실행");
        buttons.Controls.Add(run);
        if (hasToggle)
        {
            var toggle = new CheckBox { Text = ToggleOff, Font = _iconFont, Appearance = Appearance.Button, TextAlign = ContentAlignment.MiddleCenter, Size = buttonSize, Enabled = false };
            _toolTip.SetToolTip(toggle, "기능 켜기");
            toggle.CheckedChanged += (_, _) =>
            {
                toggle.Text = toggle.Checked ? ToggleOn : ToggleOff;
                _toolTip.SetToolTip(toggle, toggle.Checked ? "기능 끄기" : "기능 켜기");
            };
            buttons.Controls.Add(toggle);
        }
        var settings = new Button { Text = SettingsGlyph, Font = _iconFont, Size = buttonSize };
        _toolTip.SetToolTip(settings, "설정");
        settings.Click += (_, _) => SettingsClicked?.Invoke();
        buttons.Controls.Add(settings);

        Controls.Add(_dot, 0, 0);
        Controls.Add(name, 1, 0);
        Controls.Add(_status, 2, 0);
        Controls.Add(buttons, 3, 0);
    }

    /// <summary>점 색과 상태 글자를 바꾼다.</summary>
    public void SetState(Health health, string text)
    {
        _dot.ForeColor = TrayIcons.ColorOf(health);
        _status.Text = text;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _toolTip.Dispose();
            _iconFont.Dispose();
        }
    }
}

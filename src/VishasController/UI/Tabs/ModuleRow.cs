namespace VishasController.UI.Tabs;

/// <summary>
/// 모듈 줄 하나: 색 점, 이름, 상태 글자, 버튼(⏻ ▶️ ⚙️). 모듈마다 ▶️ 유무만 다르다.
/// ⏻·▶️는 아직 비활성이다(실제 실행은 Proc-1, 기능 켜기는 서버 제어 메시지가 필요).
/// </summary>
public sealed class ModuleRow : TableLayoutPanel
{
    const string ToggleOff = "▶️";
    const string ToggleOn = "⏹️";

    readonly Label _dot;
    readonly Label _status;

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

        var buttons = new FlowLayoutPanel { AutoSize = true, Margin = Padding.Empty, WrapContents = false };
        buttons.Controls.Add(new Button { Text = "⏻", AutoSize = true, Enabled = false });
        if (hasToggle)
        {
            var toggle = new CheckBox { Text = ToggleOff, Appearance = Appearance.Button, AutoSize = true, Enabled = false };
            toggle.CheckedChanged += (_, _) => toggle.Text = toggle.Checked ? ToggleOn : ToggleOff;
            buttons.Controls.Add(toggle);
        }
        var settings = new Button { Text = "⚙️", AutoSize = true };
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
}

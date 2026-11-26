using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace VishasController.UI;

/// <summary>트레이 아이콘 색. 신호등처럼 읽는다.</summary>
public enum Health
{
    Ok,       // 초록: 채팅을 받는 중
    Warning,  // 주황: 채팅을 못 받는 중이지만 자동 복구 중 (서버·치지직 재연결)
    Error,    // 빨강: 사람이 조치해야 함 (치지직 재로그인, 연동 꺼짐, 서버가 41 미지원)
    Idle,     // 회색: 상태 모름 (아직 등록 전 등)
}

/// <summary>상태 색 원 + 안 읽은 채팅 점 아이콘을 그려 캐시한다.</summary>
public sealed class TrayIcons : IDisposable
{
    readonly Dictionary<(Health, bool), Icon> _cache = new();

    public Icon Get(Health health, bool unread)
    {
        if (_cache.TryGetValue((health, unread), out var icon)) return icon;
        icon = Draw(health, unread);
        _cache[(health, unread)] = icon;
        return icon;
    }

    public static Color ColorOf(Health h) => h switch
    {
        Health.Ok => Color.FromArgb(46, 180, 90),
        Health.Warning => Color.FromArgb(240, 150, 30),
        Health.Error => Color.FromArgb(220, 50, 50),
        _ => Color.FromArgb(140, 140, 140),
    };

    /// <summary>준비 중(아직 연결 기능이 없음) 점 색. 탭·모듈 줄에만 쓰고 트레이 아이콘에는 쓰지 않는다.</summary>
    public static readonly Color PendingColor = Color.Black;

    static Icon Draw(Health health, bool unread)
    {
        int size = Math.Max(16, SystemInformation.SmallIconSize.Width);
        using var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // 말풍선 모양 대신 단순한 원: 작은 크기에서도 색이 잘 보인다
            float pad = size * 0.06f;
            var body = new RectangleF(pad, pad, size - pad * 2, size - pad * 2);
            using (var b = new SolidBrush(ColorOf(health))) g.FillEllipse(b, body);
            using (var p = new Pen(Color.FromArgb(60, 0, 0, 0), Math.Max(1f, size / 16f))) g.DrawEllipse(p, body);

            // 안 읽은 채팅이 있으면 가운데 흰 점
            if (unread)
            {
                float d = size * 0.42f;
                var dot = new RectangleF((size - d) / 2, (size - d) / 2, d, d);
                using var w = new SolidBrush(Color.White);
                g.FillEllipse(w, dot);
            }
        }

        // GetHicon으로 만든 핸들은 직접 해제해야 하므로 복제본을 쓰고 원본 핸들은 해제한다
        IntPtr h = bmp.GetHicon();
        try { return (Icon)Icon.FromHandle(h).Clone(); }
        finally { DestroyIcon(h); }
    }

    public void Dispose()
    {
        foreach (var icon in _cache.Values) icon.Dispose();
        _cache.Clear();
    }

    [DllImport("user32.dll")]
    static extern bool DestroyIcon(IntPtr handle);
}

using System.Media;
using VishasController.Net;
using VishasController.UI;
using LinkState = VishasController.Net.LinkState;   // WinForms에도 같은 이름이 있다

namespace VishasController;

/// <summary>
/// 트레이 상주 본체. 창 없이 시작하고, 서버 연결·치지직 상태를 아이콘 색으로,
/// 새 채팅을 알림 풍선과 아이콘의 흰 점으로 알린다.
/// </summary>
public sealed class TrayContext : ApplicationContext
{
    static readonly TimeSpan BalloonGap = TimeSpan.FromSeconds(4);   // 채팅 알림 최소 간격 (몰아서 표시)

    readonly AppSettings _settings;
    readonly SynchronizationContext _ui;
    readonly NotifyIcon _tray;
    readonly TrayIcons _icons = new();
    readonly ChatForm _chatForm;
    readonly ToolStripMenuItem _balloonItem;
    readonly ToolStripMenuItem _soundItem;
    readonly System.Windows.Forms.Timer _balloonTimer;

    SynapseClient? _client;
    LinkState _link = LinkState.Stopped;
    string _linkDetail = "";
    ChzzkState _chzzk = ChzzkState.Unknown;
    bool _everRegistered;
    int _unread;
    int _pendingChats;          // 알림 간격 동안 쌓인 채팅 수
    ChzzkChat? _pendingLast;
    bool _exiting;

    public TrayContext()
    {
        _settings = AppSettings.Load();

        // ChatForm(Control)을 먼저 만들면 UI 스레드의 SynchronizationContext가 설치된다
        _chatForm = new ChatForm();
        _chatForm.Viewed += ClearUnread;
        _ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

        _balloonItem = new ToolStripMenuItem("채팅 알림 풍선", null, (_, _) => ToggleBalloon())
        { Checked = _settings.ShowBalloon };
        _soundItem = new ToolStripMenuItem("알림음", null, (_, _) => ToggleSound())
        { Checked = _settings.PlaySound };

        var menu = new ContextMenuStrip();
        menu.Items.Add("채팅 창 열기", null, (_, _) => _chatForm.ShowAndActivate());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_balloonItem);
        menu.Items.Add(_soundItem);
        menu.Items.Add("서버 주소 설정…", null, (_, _) => OpenSettings());
        menu.Items.Add("다시 접속", null, async (_, _) => await RestartClientAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("종료", null, async (_, _) => await ExitAsync());

        _tray = new NotifyIcon
        {
            Icon = _icons.Get(Health.Idle, false),
            Text = "VishasController",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) _chatForm.ShowAndActivate(); };
        _tray.BalloonTipClicked += (_, _) => _chatForm.ShowAndActivate();

        _balloonTimer = new System.Windows.Forms.Timer { Interval = (int)BalloonGap.TotalMilliseconds };
        _balloonTimer.Tick += (_, _) => FlushPendingBalloon();

        StartClient();
        Refresh();
        ShowBalloon("VishasController", "트레이에서 실행 중입니다. 아이콘을 클릭하면 채팅 창이 열립니다.", ToolTipIcon.Info, force: true);
    }

    void ToggleBalloon()
    {
        _settings.ShowBalloon = !_settings.ShowBalloon;
        _balloonItem.Checked = _settings.ShowBalloon;
        _settings.Save();
    }

    void ToggleSound()
    {
        _settings.PlaySound = !_settings.PlaySound;
        _soundItem.Checked = _settings.PlaySound;
        _settings.Save();
    }

    // ── 서버 연결 ───────────────────────────────────────────

    void StartClient()
    {
        if (!AppSettings.TryParseServerUrl(_settings.ServerUrl, out var uri))
        {
            _link = LinkState.Stopped;
            _linkDetail = "서버 주소가 올바르지 않음";
            return;
        }
        _client = new SynapseClient(uri, _ui);
        _client.LinkStateChanged += OnLinkStateChanged;
        _client.ChatReceived += OnChat;
        _client.ChzzkStateChanged += OnChzzkState;
        _client.ServerError += (code, msg) => _chatForm.SetServerStatus($"에러 {code} {msg}", TrayIcons.ColorOf(Health.Warning));
        _client.Start();
    }

    async Task RestartClientAsync()
    {
        if (_client != null)
        {
            var old = _client;
            _client = null;
            old.LinkStateChanged -= OnLinkStateChanged;
            old.ChatReceived -= OnChat;
            old.ChzzkStateChanged -= OnChzzkState;
            await old.StopAsync();
        }
        _everRegistered = false;
        _chzzk = ChzzkState.Unknown;
        StartClient();
        Refresh();
    }

    void OnLinkStateChanged(LinkState state, string detail)
    {
        var prev = _link;
        _link = state;
        _linkDetail = detail;

        if (state != LinkState.Registered)
            _chzzk = ChzzkState.Unknown;   // 서버와 끊기면 치지직 상태도 알 수 없다

        if (prev == LinkState.Registered && state != LinkState.Registered && state != LinkState.Stopped)
            ShowBalloon("서버 연결 끊김", "다시 접속하는 동안 채팅을 받지 못합니다.", ToolTipIcon.Warning, force: true);
        else if (state == LinkState.Registered && _everRegistered)
            ShowBalloon("서버 다시 연결됨", "끊긴 동안의 채팅은 받지 못했습니다.", ToolTipIcon.Info, force: true);
        else if (state == LinkState.Unsupported && prev != LinkState.Unsupported)
            ShowBalloon("서버가 컨트롤러를 지원하지 않음", "서버에 시그니처 41이 아직 없습니다. 1분마다 다시 시도합니다.", ToolTipIcon.Error, force: true);

        if (state == LinkState.Registered) _everRegistered = true;
        Refresh();
    }

    void OnChzzkState(ChzzkState state)
    {
        var prev = _chzzk;
        _chzzk = state;
        if (prev != state)
        {
            switch (state)
            {
                case ChzzkState.Disconnected:
                    ShowBalloon("치지직 연결 끊김", "서버가 다시 연결하는 중입니다. 이 동안의 채팅은 받지 못합니다.", ToolTipIcon.Warning, force: true);
                    break;
                case ChzzkState.AuthRequired:
                    ShowBalloon("치지직 재로그인 필요", "서버 PC에서 콘솔에 나온 인증 URL로 로그인해 주세요.", ToolTipIcon.Error, force: true);
                    break;
                case ChzzkState.Disabled:
                    ShowBalloon("치지직 연동 꺼짐", "서버가 치지직 연동을 하지 않고 있습니다. 서버 설정을 확인한 뒤 서버를 다시 켜 주세요.", ToolTipIcon.Error, force: true);
                    break;
                case ChzzkState.Connected when prev is ChzzkState.Disconnected or ChzzkState.AuthRequired:
                    ShowBalloon("치지직 다시 연결됨", "채팅을 다시 받습니다.", ToolTipIcon.Info, force: true);
                    break;
            }
        }
        Refresh();
    }

    // ── 채팅 알림 ───────────────────────────────────────────

    void OnChat(ChzzkChat chat)
    {
        _chatForm.AddChat(chat);
        if (_chatForm.IsBeingWatched) return;   // 보고 있으면 알릴 필요 없음

        _unread++;
        Refresh();

        if (!_balloonTimer.Enabled)
        {
            // 첫 채팅은 바로 알리고, 이후 BalloonGap 동안 온 것은 모아서 한 번에
            ShowChatBalloon(chat, 1);
            _balloonTimer.Start();
        }
        else
        {
            _pendingChats++;
            _pendingLast = chat;
        }
    }

    void FlushPendingBalloon()
    {
        if (_pendingChats == 0 || _pendingLast == null)
        {
            _balloonTimer.Stop();
            return;
        }
        ShowChatBalloon(_pendingLast, _pendingChats);
        _pendingChats = 0;
        _pendingLast = null;
    }

    void ShowChatBalloon(ChzzkChat last, int count)
    {
        var title = count == 1 ? $"{last.Nickname}" : $"새 채팅 {count}개";
        var role = last.RoleLabel.Length > 0 ? $"[{last.RoleLabel}] " : "";
        var body = count == 1 ? role + last.DisplayContent : $"{role}{last.Nickname}: {last.DisplayContent}";
        ShowBalloon(title, body, ToolTipIcon.None, force: false);
    }

    void ClearUnread()
    {
        _unread = 0;
        _pendingChats = 0;
        _pendingLast = null;
        Refresh();
    }

    /// <param name="force">채팅 알림 끄기와 상관없이 띄운다 (상태 경고)</param>
    void ShowBalloon(string title, string text, ToolTipIcon icon, bool force)
    {
        if (_exiting) return;
        if (!force && !_settings.ShowBalloon) return;
        if (text.Length > 200) text = text[..200] + "…";
        _tray.ShowBalloonTip(5000, title, text.Length == 0 ? " " : text, icon);
        if (_settings.PlaySound) SystemSounds.Asterisk.Play();
    }

    // ── 표시 갱신 ───────────────────────────────────────────

    void Refresh()
    {
        var (health, serverText, chzzkText) = Describe();
        _tray.Icon = _icons.Get(health, _unread > 0);

        var tip = $"VishasController\n서버: {serverText}\n치지직: {chzzkText}";
        if (_unread > 0) tip += $"\n안 읽은 채팅 {_unread}";
        _tray.Text = tip.Length > 127 ? tip[..127] : tip;

        var serverHealth = _link switch
        {
            LinkState.Registered => Health.Ok,
            LinkState.Unsupported => Health.Error,
            LinkState.Stopped => Health.Idle,
            _ => Health.Warning,
        };
        _chatForm.SetServerStatus(serverText, TrayIcons.ColorOf(serverHealth));
        _chatForm.SetChzzkStatus(chzzkText, TrayIcons.ColorOf(ChzzkHealth(_chzzk)));
    }

    static Health ChzzkHealth(ChzzkState s) => s switch
    {
        ChzzkState.Connected => Health.Ok,
        ChzzkState.Disconnected => Health.Warning,
        ChzzkState.AuthRequired or ChzzkState.Disabled => Health.Error,
        _ => Health.Idle,
    };

    (Health, string server, string chzzk) Describe()
    {
        string server = _link switch
        {
            LinkState.Stopped => string.IsNullOrEmpty(_linkDetail) ? "중지" : _linkDetail,
            LinkState.Connecting => "접속 중",
            LinkState.Registering => "등록 중",
            LinkState.Registered => "연결됨",
            LinkState.Waiting => _linkDetail,
            LinkState.Unsupported => "41 미지원(서버 업데이트 필요)",
            _ => "",
        };
        string chzzk = _chzzk switch
        {
            ChzzkState.Connected => "채팅 받는 중",
            ChzzkState.Disconnected => "재연결 중(채팅 못 받음)",
            ChzzkState.AuthRequired => "재로그인 필요",
            ChzzkState.Disabled => "연동 꺼짐",
            _ => _link == LinkState.Registered ? "상태 대기" : "-",
        };

        Health health = _link switch
        {
            LinkState.Unsupported => Health.Error,
            LinkState.Stopped => _client == null ? Health.Error : Health.Idle,
            LinkState.Registered => ChzzkHealth(_chzzk),
            _ => _everRegistered ? Health.Warning : Health.Idle,
        };
        return (health, server, chzzk);
    }

    // ── 설정·종료 ───────────────────────────────────────────

    async void OpenSettings()
    {
        using var dlg = new SettingsForm(_settings.ServerUrl);
        if (dlg.ShowDialog() != DialogResult.OK) return;
        if (dlg.ServerUrl == _settings.ServerUrl) return;
        _settings.ServerUrl = dlg.ServerUrl;
        _settings.Save();
        await RestartClientAsync();
    }

    async Task ExitAsync()
    {
        if (_exiting) return;
        _exiting = true;
        _balloonTimer.Stop();
        if (_client != null) await _client.StopAsync();
        _tray.Visible = false;
        _tray.Dispose();
        _icons.Dispose();
        _chatForm.Dispose();
        ExitThread();
    }
}

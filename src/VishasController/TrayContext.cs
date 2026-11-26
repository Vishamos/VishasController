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
    readonly MainForm _mainForm;
    readonly ToolStripMenuItem _balloonItem;
    readonly ToolStripMenuItem _statusAlertItem;
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
    bool _balloonIsChat;        // 마지막으로 띄운 풍선이 채팅 알림인지 (클릭하면 치지직 탭으로)
    bool _exiting;

    public TrayContext()
    {
        _settings = AppSettings.Load();

        // MainForm(Control)을 먼저 만들면 UI 스레드의 SynchronizationContext가 설치된다
        _mainForm = new MainForm();
        _mainForm.Viewed += ClearUnread;
        _mainForm.ModuleSettingsRequested += OpenModuleSettings;
        _ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

        // 빠른 설정(명세 02 §1.2). 창 열기는 왼쪽 클릭과 같아 메뉴에 두지 않는다.
        _balloonItem = new ToolStripMenuItem("알림", null, (_, _) => ToggleBalloon())
        { Checked = _settings.ShowBalloon };
        _statusAlertItem = new ToolStripMenuItem("중요 경고", null, (_, _) => ToggleStatusAlerts())
        { Checked = _settings.ShowStatusAlerts };
        _soundItem = new ToolStripMenuItem("알림 소리", null, (_, _) => ToggleSound())
        { Checked = _settings.PlaySound };

        var menu = new ContextMenuStrip();
        menu.Items.Add(_balloonItem);
        menu.Items.Add(_statusAlertItem);
        menu.Items.Add(_soundItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("종료", null, async (_, _) => await ExitAsync());

        _tray = new NotifyIcon
        {
            Icon = _icons.Get(Health.Idle, false),
            Text = "VishasController",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) _mainForm.ShowAndActivate(); };
        _tray.BalloonTipClicked += (_, _) =>
        {
            if (_balloonIsChat) _mainForm.ShowChzzkTab();
            else _mainForm.ShowAndActivate();
        };

        _balloonTimer = new System.Windows.Forms.Timer { Interval = (int)BalloonGap.TotalMilliseconds };
        _balloonTimer.Tick += (_, _) => FlushPendingBalloon();

        StartClient();
        Refresh();
        ShowBalloon("VishasController", "트레이에서 실행 중입니다. 아이콘을 클릭하면 창이 열립니다.", ToolTipIcon.Info, status: true);
    }

    void ToggleBalloon()
    {
        _settings.ShowBalloon = !_settings.ShowBalloon;
        _balloonItem.Checked = _settings.ShowBalloon;
        _settings.Save();
    }

    void ToggleStatusAlerts()
    {
        _settings.ShowStatusAlerts = !_settings.ShowStatusAlerts;
        _statusAlertItem.Checked = _settings.ShowStatusAlerts;
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
            _mainForm.AddLog(Module.Synapse, _linkDetail);
            return;
        }
        _client = new SynapseClient(uri, _ui);
        _client.LinkStateChanged += OnLinkStateChanged;
        _client.ChatReceived += OnChat;
        _client.ChzzkStateChanged += OnChzzkState;
        _client.ServerError += (code, msg) =>
        {
            _mainForm.SetServerStatus($"에러 {code} {msg}", TrayIcons.ColorOf(Health.Warning));
            _mainForm.AddLog(Module.Synapse, $"에러 {code} {msg}");
        };
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
            ShowBalloon("서버 연결 끊김", "다시 접속하는 동안 채팅을 받지 못합니다.", ToolTipIcon.Warning, status: true);
        else if (state == LinkState.Registered && _everRegistered)
            ShowBalloon("서버 다시 연결됨", "끊긴 동안의 채팅은 받지 못했습니다.", ToolTipIcon.Info, status: true);
        else if (state == LinkState.Unsupported && prev != LinkState.Unsupported)
            ShowBalloon("서버가 컨트롤러를 지원하지 않음", "서버에 시그니처 41이 아직 없습니다. 1분마다 다시 시도합니다.", ToolTipIcon.Error, status: true);

        if (state == LinkState.Registered) _everRegistered = true;
        _mainForm.AddLog(Module.Synapse, SynapseText());
        Refresh();
    }

    void OnChzzkState(ChzzkState state)
    {
        var prev = _chzzk;
        _chzzk = state;
        if (prev != state)
        {
            _mainForm.AddLog(Module.Chzzk, "상태: " + MessageParser.StateValue(state));
            switch (state)
            {
                case ChzzkState.Disconnected:
                    ShowBalloon("치지직 연결 끊김", "서버가 다시 연결하는 중입니다. 이 동안의 채팅은 받지 못합니다.", ToolTipIcon.Warning, status: true);
                    break;
                case ChzzkState.AuthRequired:
                    ShowBalloon("치지직 재로그인 필요", "서버 PC에서 콘솔에 나온 인증 URL로 로그인해 주세요.", ToolTipIcon.Error, status: true);
                    break;
                case ChzzkState.Disabled:
                    ShowBalloon("치지직 연동 꺼짐", "서버가 치지직 연동을 하지 않고 있습니다. 서버 설정을 확인한 뒤 서버를 다시 켜 주세요.", ToolTipIcon.Error, status: true);
                    break;
                case ChzzkState.Connected when prev is ChzzkState.Disconnected or ChzzkState.AuthRequired:
                    ShowBalloon("치지직 다시 연결됨", "채팅을 다시 받습니다.", ToolTipIcon.Info, status: true);
                    break;
            }
        }
        Refresh();
    }

    // ── 채팅 알림 ───────────────────────────────────────────

    void OnChat(ChzzkChat chat)
    {
        _mainForm.AddChat(chat);
        if (_mainForm.IsBeingWatched) return;   // 보고 있으면 알릴 필요 없음

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
        ShowBalloon(title, body, ToolTipIcon.None, status: false);
    }

    void ClearUnread()
    {
        _unread = 0;
        _pendingChats = 0;
        _pendingLast = null;
        Refresh();
    }

    /// <param name="status">true면 상태 알림("중요 경고" 설정), false면 채팅 알림("알림" 설정)</param>
    void ShowBalloon(string title, string text, ToolTipIcon icon, bool status)
    {
        if (_exiting) return;
        if (status ? !_settings.ShowStatusAlerts : !_settings.ShowBalloon) return;
        if (text.Length > 200) text = text[..200] + "…";
        _tray.ShowBalloonTip(5000, title, text.Length == 0 ? " " : text, icon);
        _balloonIsChat = !status;   // status=false는 채팅 알림뿐이다
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
        _mainForm.SetServerStatus(serverText, TrayIcons.ColorOf(serverHealth));
        _mainForm.SetChzzkStatus(chzzkText, TrayIcons.ColorOf(ChzzkHealth(_chzzk)));

        // 탭·모듈 줄 점 색 (명세 02 §2.4·§2.5). 상태 표시줄·트레이 아이콘과 기준이 조금 다르다.
        var synapseHealth = _link switch
        {
            LinkState.Registered => Health.Ok,
            LinkState.Unsupported => Health.Error,
            LinkState.Stopped => _client == null ? Health.Error : Health.Idle,   // 클라이언트 없음 = 주소 형식 오류
            _ => Health.Warning,
        };
        _mainForm.SetSynapse(synapseHealth, SynapseText(), _link == LinkState.Registered);

        var chzzkTabHealth = _link != LinkState.Registered ? Health.Idle
            : _chzzk == ChzzkState.Unknown ? Health.Warning   // 상태 대기
            : ChzzkHealth(_chzzk);
        _mainForm.SetChzzk(chzzkTabHealth);
        _mainForm.SetUnread(_unread);
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
            LinkState.Waiting => WaitingText(),
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

    /// <summary>
    /// 시냅스 줄·로그에 쓰는 연결 단계 글자. 접속 중 설명(주소)과 끊긴 이유(예외 문구에 호스트가 섞일 수 있음)는 넣지 않는다.
    /// </summary>
    string SynapseText()
    {
        switch (_link)
        {
            case LinkState.Registered:
                return "연결됨 · " + _linkDetail;   // 등록 때 설명이 "연결 번호 N"
            case LinkState.Connecting:
                return "접속 중";
            default:
                return Describe().server;
        }
    }

    /// <summary>
    /// 재접속 대기 글자. 설명은 "{이유} · N초 후 다시 접속" 형식인데, 이유(예외 문구)에 서버 호스트가 섞일 수 있어
    /// 상태 표시줄·트레이 툴팁·시냅스 줄·로그 어디에도 이유는 쓰지 않는다(CLAUDE.md 안전 규칙 1).
    /// </summary>
    string WaitingText()
    {
        int i = _linkDetail.LastIndexOf(" · ", StringComparison.Ordinal);
        return i >= 0 ? "연결 끊김" + _linkDetail[i..] : "연결 끊김";
    }

    // ── 설정·종료 ───────────────────────────────────────────

    async void OpenModuleSettings(Module module)
    {
        try
        {
            var current = _settings.ModuleOf(module);
            using var dlg = new ModuleSettingsForm(module, current, module == Module.Synapse ? _settings.ServerUrl : null);
            if (dlg.ShowDialog(_mainForm) != DialogResult.OK) return;

            current.Path = dlg.ExecutablePath;
            if (module != Module.Synapse) current.AutoActivate = dlg.AutoActivate;   // 시냅스 창에는 체크박스가 없다
            current.AutoRun = dlg.AutoRun;   // 저장만 한다. 실제 자동 실행은 Proc-1
            bool urlChanged = module == Module.Synapse && dlg.ServerUrl != _settings.ServerUrl;
            if (urlChanged) _settings.ServerUrl = dlg.ServerUrl;
            _settings.Save();
            if (urlChanged) await RestartClientAsync();
        }
        catch (Exception ex)
        {
            // 설정 파일 쓰기 실패 등. 경로·주소가 섞일 수 있어 예외 종류만 보여 준다.
            MessageBox.Show(_mainForm, $"설정을 저장하지 못했습니다. ({ex.GetType().Name})", "설정",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
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
        _mainForm.Dispose();
        ExitThread();
    }
}

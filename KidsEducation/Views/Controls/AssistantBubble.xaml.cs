using CommunityToolkit.Maui.Media;
using KidsEducation.Services;

namespace KidsEducation.Views.Controls;

public partial class AssistantBubble : ContentView
{
    public static readonly BindableProperty PageKeyProperty =
        BindableProperty.Create(nameof(PageKey), typeof(string), typeof(AssistantBubble), "default",
            propertyChanged: (b, _, n) => ((AssistantBubble)b).OnPageKeyChanged((string)n));

    public string PageKey
    {
        get => (string)GetValue(PageKeyProperty);
        set => SetValue(PageKeyProperty, value);
    }

    private bool _isOpen;
    private bool _isListening;
    private CancellationTokenSource? _listenCts;

    private VoiceCommandService? _voiceService;
    private AssistantService? _assistantService;
    private NavigationService? _navService;

    public AssistantBubble()
    {
        InitializeComponent();
        ResolveServices();
    }

    private void ResolveServices()
    {
        try
        {
            var svc = IPlatformApplication.Current?.Services;
            _voiceService    = svc?.GetService<VoiceCommandService>();
            _assistantService = svc?.GetService<AssistantService>();
            _navService      = svc?.GetService<NavigationService>();
        }
        catch { }
    }

    private void OnPageKeyChanged(string key)
    {
        if (_assistantService is null) return;
        TipLabel.Text = _assistantService.GetTip(key);
    }

    // ── Baykuş butonu ───────────────────────────────────────
    private async void OnAssistantTapped(object sender, TappedEventArgs e)
    {
        if (_isOpen) { await CloseModalAsync(); return; }

        if (_assistantService is not null)
            TipLabel.Text = _assistantService.GetTip(PageKey);

        // Animasyon — panel alttan süzülür
        Overlay.IsVisible = true;
        Overlay.Opacity = 0;
        ModalPanel.IsVisible = true;
        ModalPanel.TranslationY = 600;

        await Task.WhenAll(
            Overlay.FadeTo(1, 220, Easing.CubicOut),
            ModalPanel.TranslateTo(0, 0, 260, Easing.CubicOut));

        _isOpen = true;
        TextInput.Focus();

        await AssistantButton.ScaleTo(1.12, 80, Easing.CubicOut);
        await AssistantButton.ScaleTo(1.0, 80, Easing.CubicIn);
    }

    private void OnOverlayTapped(object sender, TappedEventArgs e) =>
        _ = CloseModalAsync();

    private async Task CloseModalAsync()
    {
        if (_isListening) { _listenCts?.Cancel(); }

        await Task.WhenAll(
            Overlay.FadeTo(0, 180),
            ModalPanel.TranslateTo(0, 600, 220, Easing.CubicIn));

        Overlay.IsVisible = false;
        ModalPanel.IsVisible = false;
        _isOpen = false;
    }

    // ── Yazı ile komut ──────────────────────────────────────
    private void OnTextSubmit(object sender, EventArgs e) => _ = HandleTextCommandAsync();

    private void OnSendTapped(object sender, TappedEventArgs e) => _ = HandleTextCommandAsync();

    private async Task HandleTextCommandAsync()
    {
        var text = TextInput.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(text)) return;

        TextInput.Text = "";
        bool navigated = await TryNavigateFromText(text);

        if (navigated)
        {
            TipLabel.Text = "Gidiyorum!";
            await Task.Delay(400);
            await CloseModalAsync();
        }
        else
        {
            TipLabel.Text = $"\"{text}\" komutunu anlayamadim. Menuden secebilirsin.";
        }
    }

    // ── Sesli komut ─────────────────────────────────────────
    private async void OnMicTapped(object sender, TappedEventArgs e)
    {
        if (_voiceService is null)
        {
            TipLabel.Text = "Sesli komut bu cihazda kullanılamıyor.";
            return;
        }

        if (_isListening)
        {
            _listenCts?.Cancel();
            return;
        }

        // İzin iste
        try
        {
            var stt = IPlatformApplication.Current?.Services.GetService<ISpeechToText>();
            if (stt is not null)
            {
                var granted = await stt.RequestPermissions();
                if (!granted)
                {
                    TipLabel.Text = "Mikrofon iznine ihtiyacım var. Ayarlar → Uygulama İzinleri.";
                    return;
                }
            }
        }
        catch { }

        _isListening = true;
        _listenCts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        SetListeningState(true);

        try
        {
            var result = await _voiceService.ListenAndExecuteAsync(_listenCts.Token);

            if (result.CommandFound)
            {
                TipLabel.Text = $"Anladim! \"{result.MatchedKeyword}\"";
                await Task.Delay(500);
                await CloseModalAsync();
            }
            else if (!string.IsNullOrWhiteSpace(result.RecognizedText))
            {
                TipLabel.Text = $"\"{result.RecognizedText}\" anlasilmadi, tekrar dene.";
            }
            else
            {
                TipLabel.Text = "Seni duymadim. Tekrar soyler misin?";
            }
        }
        catch (OperationCanceledException)
        {
            TipLabel.Text = "Dinleme iptal edildi.";
        }
        catch (Exception ex)
        {
            TipLabel.Text = $"Hata: {ex.Message}";
        }
        finally
        {
            _isListening = false;
            _listenCts = null;
            SetListeningState(false);
        }
    }

    // ── Hızlı kısayollar ────────────────────────────────────
    private async void OnShortcutTapped(object sender, TappedEventArgs e)
    {
        // sender = Border; CommandParameter TapGestureRecognizer'da tanımlı
        var param = "";
        if (sender is View v)
            param = v.GestureRecognizers.OfType<TapGestureRecognizer>()
                      .FirstOrDefault()?.CommandParameter as string ?? "";

        if (string.IsNullOrEmpty(param)) return;
        await NavigateByParam(param);
        await CloseModalAsync();
    }

    private Task NavigateByParam(string param) => param switch
    {
        "__games"    => _navService?.GoToGamesAsync() ?? Task.CompletedTask,
        "__topics"   => Shell.Current.GoToAsync("learningmodules"),
        "__home"     => Shell.Current.GoToAsync("//home"),
        "__parental" => _navService?.GoToParentalAsync() ?? Task.CompletedTask,
        _            => _navService?.GoToCategoryAsync(param) ?? Task.CompletedTask
    };

    // ── Metin eşleştirme ────────────────────────────────────
    private async Task<bool> TryNavigateFromText(string text)
    {
        var lower = text.ToLowerInvariant();

        var map = new (string[] keys, string param)[]
        {
            (["hayvan", "hayvanlar"],                "animals"),
            (["meyve", "meyveler"],                  "fruits"),
            (["sebze", "sebzeler"],                  "vegetables"),
            (["renk", "renkler"],                    "colors"),
            (["şekil", "şekiller"],                  "shapes"),
            (["araç", "taşıt"],                      "vehicles"),
            (["sayı", "sayılar"],                    "numbers"),
            (["harf", "harfler"],                    "letters"),
            (["duygu", "duygular"],                  "emotions"),
            (["gezegen", "gezegenler"],              "planets"),
            (["şehir", "şehirler", "il", "iller"],  "cities"),
            (["ülke", "ülkeler"],                    "countries"),
            (["meslek", "meslekler"],                "professions"),
            (["doğa"],                               "nature"),
            (["nesne", "nesneler"],                  "objects"),
            (["zıt", "karşıt", "zıtlar"],           "opposites"),
            (["oyun", "oyunlar", "oyna"],            "__games"),
            (["konu", "konular", "öğren"],           "__topics"),
            (["ana", "ev", "home"],                  "__home"),
            (["ayar", "ebeveyn", "veli"],            "__parental"),
        };

        foreach (var (keys, param) in map)
        {
            if (keys.Any(k => lower.Contains(k)))
            {
                await NavigateByParam(param);
                return true;
            }
        }
        return false;
    }

    private void SetListeningState(bool listening)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MicLabel.Text  = listening ? "Dinleniyor… (durdurmak için dokun)" : "Sesli komut ver";
            MicButton.BackgroundColor = listening
                ? Color.FromArgb("#FFEBEE")
                : Color.FromArgb("#EEF0FF");
        });
    }
}

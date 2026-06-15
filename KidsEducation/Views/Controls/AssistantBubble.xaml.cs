using System.Globalization;
using CommunityToolkit.Maui.Media;
using KidsEducation.Services;

namespace KidsEducation.Views.Controls;

public partial class AssistantBubble : ContentView
{
    private const string VoiceMutedPreferenceKey = "assistant_voice_muted";

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
    private bool _isMuted;
    private CancellationTokenSource? _listenCts;

    // Sürükle-bırak konumlandırma
    private double _savedX;
    private double _savedY;
    private bool _isDragging;

    private VoiceCommandService? _voiceService;
    private AssistantService? _assistantService;
    private NavigationService? _navService;
    private AudioService? _audioService;

    public AssistantBubble()
    {
        InitializeComponent();
        _isMuted = Preferences.Default.Get(VoiceMutedPreferenceKey, false);
        _savedX = Preferences.Default.Get("assistant_drag_x", 0.0);
        _savedY = Preferences.Default.Get("assistant_drag_y", 0.0);
        ResolveServices();
        UpdateSoundToggle();
        ApplyClosedHostMode();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        ResolveServices();
    }

    private void ResolveServices()
    {
        try
        {
            var svc = IPlatformApplication.Current?.Services;
            _voiceService = svc?.GetService<VoiceCommandService>();
            _assistantService = svc?.GetService<AssistantService>();
            _navService = svc?.GetService<NavigationService>();
            _audioService = svc?.GetService<AudioService>();
        }
        catch
        {
        }
    }

    private void OnPageKeyChanged(string key)
    {
        if (_assistantService is null) return;
        TipLabel.Text = _assistantService.GetTip(key);
    }

    private void OnButtonPanned(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _isDragging = false;
                break;
            case GestureStatus.Running:
                _isDragging = true;
                // ContentView'ın kendisini taşı — hit area da görselle birlikte gelir
                TranslationX = _savedX + e.TotalX;
                TranslationY = _savedY + e.TotalY;
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (_isDragging)
                {
                    _savedX = TranslationX;
                    _savedY = TranslationY;
                    Preferences.Default.Set("assistant_drag_x", _savedX);
                    Preferences.Default.Set("assistant_drag_y", _savedY);
                }
                _isDragging = false;
                break;
        }
    }

    private void OnCollapseTapped(object sender, TappedEventArgs e) =>
        _ = CloseModalAsync();

    private async void OnAssistantTapped(object sender, TappedEventArgs e)
    {
        // Sürükleme bittikten hemen sonra gelen tap'i yoksay
        if (_isDragging) return;

        if (_isOpen)
        {
            await CloseModalAsync();
            return;
        }

        var tip = _assistantService?.GetTip(PageKey)
            ?? "Merhaba, yazabilir ya da mikrofonla komut verebilirsin.";
        TipLabel.Text = tip;

        ApplyOpenHostMode();
        Overlay.IsVisible = true;
        Overlay.Opacity = 0;
        ModalPanel.IsVisible = true;
        ModalPanel.TranslationY = 620;

        await Task.WhenAll(
            Overlay.FadeToAsync(1, 220, Easing.CubicOut),
            ModalPanel.TranslateToAsync(0, 0, 260, Easing.CubicOut));

        _isOpen = true;
        TextInput.Focus();

        await AssistantButton.ScaleToAsync(1.12, 80, Easing.CubicOut);
        await AssistantButton.ScaleToAsync(1.0, 80, Easing.CubicIn);
        await SpeakAsync(tip);
    }

    private void OnOverlayTapped(object sender, TappedEventArgs e) =>
        _ = CloseModalAsync();

    private async Task CloseModalAsync()
    {
        if (_isListening)
        {
            _listenCts?.Cancel();
        }

        await Task.WhenAll(
            Overlay.FadeToAsync(0, 180),
            ModalPanel.TranslateToAsync(0, 620, 220, Easing.CubicIn));

        Overlay.IsVisible = false;
        ModalPanel.IsVisible = false;
        _isOpen = false;
        ApplyClosedHostMode();
    }

    private void ApplyOpenHostMode()
    {
        // Modal açıkken tam ekran ol, önceki translation sıfırla
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Fill;
        WidthRequest = -1;
        HeightRequest = -1;
        Margin = Thickness.Zero;
        TranslationX = 0;
        TranslationY = 0;
        InputTransparent = false;
    }

    private void ApplyClosedHostMode()
    {
        // Küçük kutu olarak sağ alt köşede dur
        HorizontalOptions = LayoutOptions.End;
        VerticalOptions = LayoutOptions.End;
        WidthRequest = 80;
        HeightRequest = 80;
        Margin = Thickness.Zero;
        InputTransparent = false;
        // ContentView'ın kendisini kaydedilmiş konuma taşı (hit area da taşınır)
        TranslationX = _savedX;
        TranslationY = _savedY;
        AssistantButton.TranslationX = 0;
        AssistantButton.TranslationY = 0;
    }

    private void OnTextSubmit(object sender, EventArgs e) => _ = HandleTextCommandAsync();

    private void OnSendTapped(object sender, TappedEventArgs e) => _ = HandleTextCommandAsync();

    private async Task HandleTextCommandAsync()
    {
        var text = TextInput.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(text))
        {
            await ReplyAsync("Ne yapmak istediğini yazabilir ya da mikrofonla söyleyebilirsin.");
            return;
        }

        TextInput.Text = "";

        if (TryGetAssistantAnswer(text, out var answer))
        {
            await ReplyAsync(answer);
            return;
        }

        if (TryResolveNavigation(text, out var param, out var title))
        {
            await ReplyAsync($"{title} bölümünü açıyorum.");
            await Task.Delay(350);
            await NavigateByParam(param);
            await CloseModalAsync();
            return;
        }

        await ReplyAsync($"\"{text}\" komutunu anlayamadım. Hayvanlar, oyunlar, şarkılar, konular veya ana sayfa diyebilirsin.");
    }

    private async void OnMicTapped(object sender, TappedEventArgs e)
    {
        if (_voiceService is null)
        {
            await ReplyAsync("Sesli komut bu cihazda kullanılamıyor.");
            return;
        }

        if (_isListening)
        {
            _listenCts?.Cancel();
            return;
        }

        try
        {
            var stt = IPlatformApplication.Current?.Services.GetService<ISpeechToText>();
            if (stt is not null)
            {
                var granted = await stt.RequestPermissions();
                if (!granted)
                {
                    await ReplyAsync("Mikrofon iznine ihtiyacım var. Ayarlardan mikrofon iznini açmalısın.");
                    return;
                }
            }
        }
        catch
        {
        }

        _isListening = true;
        _listenCts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        SetListeningState(true);
        TipLabel.Text = "Seni dinliyorum. Hayvanlar, oyunlar, şarkılar veya ana sayfa diyebilirsin.";

        try
        {
            var result = await _voiceService.ListenAndExecuteAsync(_listenCts.Token);

            if (result.CommandFound)
            {
                var title = GetParamDisplayName(result.MatchedRoute ?? result.MatchedKeyword ?? "");
                await ReplyAsync($"{title} bölümünü açıyorum.");
                await Task.Delay(500);
                await CloseModalAsync();
            }
            else if (!string.IsNullOrWhiteSpace(result.RecognizedText) &&
                     TryResolveNavigation(result.RecognizedText, out var param, out var title))
            {
                await ReplyAsync($"Anladım, {title} bölümünü açıyorum.");
                await Task.Delay(350);
                await NavigateByParam(param);
                await CloseModalAsync();
            }
            else if (!string.IsNullOrWhiteSpace(result.RecognizedText))
            {
                await ReplyAsync($"\"{result.RecognizedText}\" dedin ama uygun bir bölüm bulamadım. Tekrar dener misin?");
            }
            else
            {
                await ReplyAsync("Seni duyamadım. Biraz daha yakından tekrar söyleyebilir misin?");
            }
        }
        catch (OperationCanceledException)
        {
            TipLabel.Text = "Dinleme iptal edildi.";
        }
        catch (Exception ex)
        {
            await ReplyAsync($"Sesli komutta sorun oldu: {ex.Message}");
        }
        finally
        {
            _isListening = false;
            _listenCts = null;
            SetListeningState(false);
        }
    }

    private async void OnSoundToggleTapped(object sender, TappedEventArgs e)
    {
        _isMuted = !_isMuted;
        Preferences.Default.Set(VoiceMutedPreferenceKey, _isMuted);
        UpdateSoundToggle();

        if (_isMuted)
        {
            _audioService?.StopSpeech();
            TipLabel.Text = "Sesim kapalı. Yine de buradan yazılı cevap vereceğim.";
        }
        else
        {
            await ReplyAsync("Sesim açık. Artık cevapları sesli de okuyacağım.");
        }
    }

    private async void OnShortcutTapped(object sender, TappedEventArgs e)
    {
        var param = "";
        if (sender is View view)
        {
            param = view.GestureRecognizers.OfType<TapGestureRecognizer>()
                .FirstOrDefault()?.CommandParameter as string ?? "";
        }

        if (string.IsNullOrWhiteSpace(param)) return;

        await ReplyAsync($"{GetParamDisplayName(param)} bölümünü açıyorum.");
        await Task.Delay(300);
        await NavigateByParam(param);
        await CloseModalAsync();
    }

    private Task NavigateByParam(string param) => param switch
    {
        "__games" => _navService?.GoToGamesAsync() ?? Task.CompletedTask,
        "__topics" => Shell.Current.GoToAsync("learningmodules"),
        "__home" => Shell.Current.GoToAsync("//home"),
        "__parental" => _navService?.GoToParentalAsync() ?? Task.CompletedTask,
        "__songs" => _navService?.GoToSongsAsync() ?? Task.CompletedTask,
        "__adventure" => _navService?.GoToAdventureMapAsync() ?? Task.CompletedTask,
        "__back" => Shell.Current.GoToAsync(".."),
        "__connectdots" => Shell.Current.GoToAsync("connectdots"),
        "__drawing" => Shell.Current.GoToAsync("drawinggame"),
        _ => _navService?.GoToCategoryAsync(param) ?? Task.CompletedTask
    };

    private static bool TryGetAssistantAnswer(string text, out string answer)
    {
        var lower = NormalizeCommand(text);

        if (ContainsAny(lower, "merhaba", "selam"))
        {
            answer = "Merhaba. Ben Asistan Baykuş. Sana konuları, oyunları ve şarkıları hızlıca açabilirim.";
            return true;
        }

        if (ContainsAny(lower, "ne yapabilirsin", "yardim", "yardım", "komut", "asistan"))
        {
            answer = "Bana hayvanlar, meyveler, sayılar, oyunlar, şarkılar, konular, ana sayfa veya ayarlar diyebilirsin.";
            return true;
        }

        if (ContainsAny(lower, "sesini kapat", "sessiz", "sus"))
        {
            answer = "Sesimi sağ üstteki Ses açık düğmesinden kapatabilirsin.";
            return true;
        }

        answer = "";
        return false;
    }

    private static bool TryResolveNavigation(string text, out string param, out string title)
    {
        var lower = NormalizeCommand(text);
        var map = new (string[] keys, string param, string title)[]
        {
            (["hayvan", "hayvanlar"], "animals", "Hayvanlar"),
            (["meyve", "meyveler"], "fruits", "Meyveler"),
            (["sebze", "sebzeler"], "vegetables", "Sebzeler"),
            (["renk", "renkler"], "colors", "Renkler"),
            (["sekil", "sekiller"], "shapes", "Şekiller"),
            (["arac", "araclar", "tasit", "tasitlar"], "vehicles", "Araçlar"),
            (["sayi", "sayilar"], "numbers", "Sayılar"),
            (["harf", "harfler"], "letters", "Harfler"),
            (["duygu", "duygular"], "emotions", "Duygular"),
            (["gezegen", "gezegenler"], "planets", "Gezegenler"),
            (["sehir", "sehirler", "il", "iller"], "cities", "İller"),
            (["ulke", "ulkeler"], "countries", "Ülkeler"),
            (["meslek", "meslekler"], "professions", "Meslekler"),
            (["doga"], "nature", "Doğa"),
            (["nesne", "nesneler"], "objects", "Nesneler"),
            (["zit", "karsit", "zitlar"], "opposites", "Zıtlar"),
            (["oyun", "oyunlar", "oyna"], "__games", "Oyunlar"),
            (["konu", "konular", "ogren", "öğren"], "__topics", "Konular"),
            (["sarki", "sarkilar", "muzik"], "__songs", "Şarkılar"),
            (["macera", "harita"], "__adventure", "Macera"),
            (["ana", "ana sayfa", "ev", "home"], "__home", "Ana sayfa"),
            (["ayar", "ayarlar", "ebeveyn", "veli"], "__parental", "Ayarlar"),
            (["geri", "geri don"], "__back", "Geri"),
            (["nokta", "nokta birlestir"], "__connectdots", "Nokta birleştir"),
            (["cizim", "ciz"], "__drawing", "Çizim")
        };

        foreach (var (keys, route, displayTitle) in map)
        {
            if (keys.Any(k => lower.Contains(NormalizeCommand(k))))
            {
                param = route;
                title = displayTitle;
                return true;
            }
        }

        param = "";
        title = "";
        return false;
    }

    private async Task ReplyAsync(string message, bool speak = true)
    {
        TipLabel.Text = message;
        if (speak)
        {
            await SpeakAsync(message);
        }
    }

    private async Task SpeakAsync(string message)
    {
        if (_isMuted || _audioService is null || string.IsNullOrWhiteSpace(message)) return;
        await _audioService.SpeakTextAsync(message.Replace("\"", ""));
    }

    private void SetListeningState(bool listening)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MicLabel.Text = listening ? "Dinleniyor... durdurmak için dokun" : "Sesli komut ver";
            MicButton.BackgroundColor = listening
                ? Color.FromArgb("#FFEBEE")
                : Color.FromArgb("#EEF0FF");
        });
    }

    private void UpdateSoundToggle()
    {
        SoundLabel.Text = _isMuted ? "Sessiz" : "Ses açık";
        SoundToggleButton.BackgroundColor = _isMuted
            ? Color.FromArgb("#F3F4F6")
            : Color.FromArgb("#EEF0FF");
        SoundIcon.Opacity = _isMuted ? 0.45 : 1.0;
    }

    private static string GetParamDisplayName(string param) => param switch
    {
        "animals" => "Hayvanlar",
        "fruits" => "Meyveler",
        "vegetables" => "Sebzeler",
        "colors" => "Renkler",
        "shapes" => "Şekiller",
        "vehicles" => "Araçlar",
        "numbers" => "Sayılar",
        "letters" => "Harfler",
        "emotions" => "Duygular",
        "planets" => "Gezegenler",
        "cities" => "İller",
        "countries" => "Ülkeler",
        "professions" => "Meslekler",
        "nature" => "Doğa",
        "objects" => "Nesneler",
        "opposites" => "Zıtlar",
        "__games" => "Oyunlar",
        "__topics" => "Konular",
        "__home" => "Ana sayfa",
        "__parental" => "Ayarlar",
        "__songs" => "Şarkılar",
        "__adventure" => "Macera",
        "__back" => "Geri",
        "__connectdots" => "Nokta birleştir",
        "__drawing" => "Çizim",
        _ => string.IsNullOrWhiteSpace(param) ? "İlgili" : param
    };

    private static bool ContainsAny(string source, params string[] keys) =>
        keys.Any(k => source.Contains(NormalizeCommand(k)));

    private static string NormalizeCommand(string text)
    {
        return text.Trim()
            .ToLower(new CultureInfo("tr-TR"))
            .Replace('ı', 'i')
            .Replace('ğ', 'g')
            .Replace('ü', 'u')
            .Replace('ş', 's')
            .Replace('ö', 'o')
            .Replace('ç', 'c');
    }
}

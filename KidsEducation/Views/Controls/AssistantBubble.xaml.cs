using CommunityToolkit.Maui.Media;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.Views.Controls;

public partial class AssistantBubble : ContentView
{
    private const string VoiceMutedPreferenceKey = "assistant_voice_muted";
    private static readonly NavigationIntent[] NavigationIntents =
    [
        new(["hayvan", "hayvanlar"], "animals", "Hayvanlar"),
        new(["meyve", "meyveler"], "fruits", "Meyveler"),
        new(["sebze", "sebzeler"], "vegetables", "Sebzeler"),
        new(["renk", "renkler"], "colors", "Renkler"),
        new(["sekil", "sekiller"], "shapes", "Sekiller"),
        new(["arac", "araclar", "tasit", "tasitlar"], "vehicles", "Araclar"),
        new(["sayi", "sayilar"], "numbers", "Sayilar"),
        new(["harf", "harfler"], "letters", "Harfler"),
        new(["duygu", "duygular"], "emotions", "Duygular"),
        new(["gezegen", "gezegenler"], "planets", "Gezegenler"),
        new(["sehir", "sehirler", "il", "iller"], "cities", "Iller"),
        new(["ulke", "ulkeler"], "countries", "Ulkeler"),
        new(["meslek", "meslekler"], "professions", "Meslekler"),
        new(["doga"], "nature", "Doga"),
        new(["nesne", "nesneler", "esya", "esyalar"], "objects", "Esyalar"),
        new(["zit", "karsit", "zitlar"], "opposites", "Zitlar"),
        new(["trafik"], "traffic", "Trafik"),
        new(["hava", "hava durumu"], "weather", "Hava Durumu"),
        new(["mevsim", "mevsimler"], "seasons", "Mevsimler"),
        new(["oyun", "oyunlar", "oyna"], "__games", "Oyunlar"),
        new(["gunluk gorev", "gunluk plan", "hedef"], "__dailygoal", "Gunluk Gorevler"),
        new(["ilerleme", "rapor"], "__progress", "Ilerleme"),
        new(["basari", "basarilar", "basarim"], "__achievements", "Basarimlar"),
        new(["masal", "masallar"], "__tales", "Masallar"),
        new(["konu", "konular", "ogren"], "__topics", "Konular"),
        new(["sarki", "sarkilar", "muzik"], "__songs", "Sarkilar"),
        new(["macera", "harita"], "__adventure", "Macera"),
        new(["ana", "ana sayfa", "ev", "home"], "__home", "Ana sayfa"),
        new(["ayar", "ayarlar", "ebeveyn", "veli"], "__parental", "Ayarlar"),
        new(["geri", "geri don"], "__back", "Geri"),
        new(["nokta", "nokta birlestir"], "__connectdots", "Nokta Birlestir"),
        new(["cizim", "ciz"], "__drawing", "Cizim"),
        new(["boyama", "resim boya"], "__coloring", "Boyama"),
        new(["sekil boyama"], "__shapecoloring", "Sekil Boyama"),
        new(["matematik", "toplama", "cikarma"], "__math", "Matematik"),
        new(["eslestirme", "eslestir"], "__matching", "Eslestirme"),
        new(["hafiza", "kart"], "__memory", "Hafiza"),
        new(["ses", "sesli tahmin"], "__sound", "Sesli Tahmin")
    ];

    public static readonly BindableProperty PageKeyProperty =
        BindableProperty.Create(nameof(PageKey), typeof(string), typeof(AssistantBubble), "default",
            propertyChanged: (bindable, _, newValue) => ((AssistantBubble)bindable).OnPageKeyChanged((string)newValue));

    public string PageKey
    {
        get => (string)GetValue(PageKeyProperty);
        set => SetValue(PageKeyProperty, value);
    }

    private bool _isOpen;
    private bool _isListening;
    private bool _isMuted;
    private CancellationTokenSource? _listenCts;
    private double _savedX;
    private double _savedY;
    private bool _isDragging;
    private List<NavigationIntent> _activeSuggestions = new();
    private bool _suggestionCameFromVoice;

    private VoiceCommandService? _voiceService;
    private AssistantService? _assistantService;
    private NavigationService? _navigationService;
    private AudioService? _audioService;
    private ProfileService? _profileService;

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
            var services = IPlatformApplication.Current?.Services;
            _voiceService = services?.GetService<VoiceCommandService>();
            _assistantService = services?.GetService<AssistantService>();
            _navigationService = services?.GetService<NavigationService>();
            _audioService = services?.GetService<AudioService>();
            _profileService = services?.GetService<ProfileService>();
        }
        catch
        {
        }
    }

    private void OnPageKeyChanged(string key)
    {
        var tip = _assistantService?.GetTip(key) ?? "Sana yardim etmek icin buradayim.";
        MainThread.BeginInvokeOnMainThread(() => TipLabel.Text = tip);
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

    private void OnCollapseTapped(object sender, TappedEventArgs e) => _ = CloseModalAsync();

    private async void OnAssistantTapped(object sender, TappedEventArgs e)
    {
        if (_isDragging) return;

        if (_isOpen)
        {
            await CloseModalAsync();
            return;
        }

        var tip = _assistantService?.GetTip(PageKey)
            ?? "Merhaba, yazabilir ya da mikrofonla komut verebilirsin.";

        ApplyOpenHostMode();
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ClearSuggestions();
            TipLabel.Text = tip;
            Overlay.IsVisible = true;
            Overlay.Opacity = 0;
            ModalPanel.IsVisible = true;
            ModalPanel.TranslationY = 620;
        });

        await Task.WhenAll(
            Overlay.FadeToAsync(1, 220, Easing.CubicOut),
            ModalPanel.TranslateToAsync(0, 0, 260, Easing.CubicOut));

        _isOpen = true;
        await MainThread.InvokeOnMainThreadAsync(() => TextInput.Focus());

        await AssistantButton.ScaleToAsync(1.12, 80, Easing.CubicOut);
        await AssistantButton.ScaleToAsync(1.0, 80, Easing.CubicIn);
        await SpeakAsync(tip);
    }

    private void OnOverlayTapped(object sender, TappedEventArgs e) => _ = CloseModalAsync();

    private async Task CloseModalAsync()
    {
        if (_isListening)
            _listenCts?.Cancel();

        await Task.WhenAll(
            Overlay.FadeToAsync(0, 180),
            ModalPanel.TranslateToAsync(0, 620, 220, Easing.CubicIn));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ClearSuggestions();
            Overlay.IsVisible = false;
            ModalPanel.IsVisible = false;
        });

        _isOpen = false;
        ApplyClosedHostMode();
    }

    private void ApplyOpenHostMode()
    {
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
        HorizontalOptions = LayoutOptions.End;
        VerticalOptions = LayoutOptions.End;
        WidthRequest = 80;
        HeightRequest = 80;
        Margin = Thickness.Zero;
        InputTransparent = false;
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
            await ReplyAsync("Ne yapmak istedigini yazabilir ya da mikrofonla soyleyebilirsin.");
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(() => TextInput.Text = "");

        if (TryGetAssistantAnswer(text, out var answer))
        {
            ClearSuggestions();
            await ReplyAsync(answer);
            return;
        }

        if (TryResolveNavigation(text, out var route, out var title))
        {
            ClearSuggestions();
            await ReplyAsync($"{title} bolumunu aciyorum.");
            await Task.Delay(250);
            await NavigateByParam(route);
            await CloseModalAsync();
            return;
        }

        var suggestions = GetNavigationSuggestions(text);
        if (suggestions.Count > 0)
        {
            await ShowSuggestionsAsync(suggestions, "Tam emin olamadim. Belki bunlardan birini istedin.", cameFromVoice: false);
            return;
        }

        ClearSuggestions();
        var example = _assistantService?.GetVoiceExampleCommand(PageKey) ?? "\"Ana sayfa\" veya \"Hayvanlar\" de.";
        await ReplyAsync($"Bunu anlayamadim. {example}");
    }

    private async void OnMicTapped(object sender, TappedEventArgs e)
    {
        if (_voiceService is null)
        {
            await ReplyAsync("Sesli komut bu cihazda kullanilamiyor.");
            return;
        }

        if (_isListening)
        {
            _listenCts?.Cancel();
            return;
        }

        try
        {
            var speechToText = IPlatformApplication.Current?.Services.GetService<ISpeechToText>();
            if (speechToText is not null)
            {
                var granted = await speechToText.RequestPermissions();
                if (!granted)
                {
                    await ReplyAsync("Mikrofon iznine ihtiyacim var. Ayarlardan mikrofon iznini acabilirsin.");
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

        var prompt = _assistantService?.GetVoiceExampleCommand(PageKey)
            ?? "\"Hayvanlar\", \"Oyunlar\" veya \"Ana sayfa\" diyebilirsin.";
        await SetTipTextAsync($"Seni dinliyorum. {prompt}");

        try
        {
            var result = await _voiceService.ListenAsync(_listenCts.Token);

            if (result.CommandFound && !string.IsNullOrWhiteSpace(result.MatchedRoute))
            {
                ClearSuggestions();
                var displayName = result.SuggestedDisplayName ?? GetParamDisplayName(result.MatchedRoute);
                await ReplyAsync($"Anladim. {displayName} bolumunu aciyorum.");
                await Task.Delay(250);
                await NavigateByParam(result.MatchedRoute);
                await CloseModalAsync();
            }
            else if (!string.IsNullOrWhiteSpace(result.RecognizedText))
            {
                var suggestions = GetNavigationSuggestions(result.RecognizedText);
                if (suggestions.Count > 0)
                {
                    await ShowSuggestionsAsync(suggestions, $"\"{result.RecognizedText}\" dedin. Galiba bunlardan birini istedin.", cameFromVoice: true);
                }
                else
                {
                    ClearSuggestions();
                    await ReplyAsync($"\"{result.RecognizedText}\" dedin ama uygun bir bolum bulamadim. Tekrar dener misin?");
                }
            }
            else
            {
                ClearSuggestions();
                await ReplyAsync("Seni duyamadim. Biraz daha yakindan tekrar soyleyebilir misin?");
            }
        }
        catch (OperationCanceledException)
        {
            await SetTipTextAsync("Dinleme iptal edildi.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AssistantBubble] Voice command failed: {ex}");
            await ReplyAsync("Sesli komutta kucuk bir sorun oldu. Tekrar dener misin?", speak: false);
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
            await SetTipTextAsync("Sesim kapali. Yazili olarak yardim etmeye devam ederim.");
        }
        else
        {
            await ReplyAsync("Sesim acik. Artik cevaplari sesli de okuyacagim.");
        }
    }

    private async void OnShortcutTapped(object sender, TappedEventArgs e)
    {
        string route = "";
        if (sender is TapGestureRecognizer recognizer)
        {
            route = recognizer.CommandParameter as string ?? "";
        }
        else if (sender is View view)
        {
            route = view.GestureRecognizers
                .OfType<TapGestureRecognizer>()
                .FirstOrDefault()?.CommandParameter as string ?? "";
        }

        if (string.IsNullOrWhiteSpace(route))
            return;

        ClearSuggestions();
        await ReplyAsync($"{GetParamDisplayName(route)} bolumunu aciyorum.");
        await Task.Delay(200);
        await NavigateByParam(route);
        await CloseModalAsync();
    }

    private async void OnSuggestionConfirmTapped(object sender, TappedEventArgs e)
    {
        if (_activeSuggestions.Count == 0)
            return;

        await ExecuteSuggestionAsync(_activeSuggestions[0]);
    }

    private async void OnSuggestionRetryTapped(object sender, TappedEventArgs e)
    {
        ClearSuggestions();

        if (_suggestionCameFromVoice && _voiceService is not null && !_isListening)
        {
            await ReplyAsync("Tamam, seni yeniden dinliyorum.");
            OnMicTapped(sender, e);
            return;
        }

        await ReplyAsync("Tekrar yazabilir ya da mikrofonla bir kez daha soyleyebilirsin.", speak: false);
        await MainThread.InvokeOnMainThreadAsync(() => TextInput.Focus());
    }

    private Task NavigateByParam(string param)
    {
        return MainThread.InvokeOnMainThreadAsync(() => param switch
        {
            "__games" => _navigationService?.GoToGamesAsync() ?? Task.CompletedTask,
            "__dailygoal" => Shell.Current.GoToAsync("dailygoal"),
            "__progress" => Shell.Current.GoToAsync("//progress"),
            "__achievements" => Shell.Current.GoToAsync("//achievements"),
            "__tales" => Shell.Current.GoToAsync("tales"),
            "__topics" => Shell.Current.GoToAsync("learningmodules"),
            "__home" => Shell.Current.GoToAsync("//home"),
            "__parental" => _navigationService?.GoToParentalAsync() ?? Task.CompletedTask,
            "__songs" => _navigationService?.GoToSongsAsync() ?? Task.CompletedTask,
            "__adventure" => _navigationService?.GoToAdventureMapAsync() ?? Task.CompletedTask,
            "__back" => Shell.Current.GoToAsync(".."),
            "__connectdots" => Shell.Current.GoToAsync("connectdots"),
            "__drawing" => Shell.Current.GoToAsync("drawinggame"),
            "__coloring" => Shell.Current.GoToAsync("coloringgame"),
            "__shapecoloring" => Shell.Current.GoToAsync("shapecoloring"),
            "__math" => Shell.Current.GoToAsync("mathgame"),
            "__matching" => Shell.Current.GoToAsync("matchinggame?categoryId=animals"),
            "__memory" => Shell.Current.GoToAsync("memorygamev2?categoryId=animals"),
            "__sound" => Shell.Current.GoToAsync("soundgame?categoryId=animals"),
            _ => _navigationService?.GoToCategoryAsync(param) ?? Task.CompletedTask
        });
    }

    private bool TryGetAssistantAnswer(string text, out string answer)
    {
        var lower = NormalizeCommand(text);
        var profile = _profileService?.GetActiveProfile();
        var dailyGoal = profile is null ? null : _profileService?.GetDailyGoal(profile);

        if (ContainsAny(lower, "merhaba", "selam"))
        {
            answer = profile is null
                ? "Merhaba. Ben Asistan Baykus. Sana oyunlari, konulari ve masallari hizlica acabilirim."
                : $"Merhaba {profile.Name}. Sana oyunlari, konulari ve gunluk gorevlerini hizlica acabilirim.";
            return true;
        }

        if (ContainsAny(lower, "ne yapabilirsin", "yardim", "komut", "neler var"))
        {
            answer = "Hayvanlar, meyveler, sayilar, oyunlar, sarkilar, konular, gunluk gorevler, masallar veya ana sayfa diyebilirsin.";
            return true;
        }

        if (ContainsAny(lower, "hangi oyunu onerirsin", "hangi oyun", "oyun oner", "ne oynayayim"))
        {
            answer = dailyGoal is not null && dailyGoal.CompletedCount < dailyGoal.TotalCount
                ? "Bugun once kisa bir eslestirme ya da sesli tahmin oyunu iyi gider. Hem gunluk hedefe yaklasirsin hem hizli biter."
                : "Hafiza, matematik veya boyama arasindan birini acabiliriz. Hafiza oyunu hizli bir baslangic icin cok uygun.";
            return true;
        }

        if (ContainsAny(lower, "bugun ne yapayim", "bugun ne ogreneyim", "ne onerirsin", "ne yapmaliyim"))
        {
            if (dailyGoal is not null)
            {
                answer = dailyGoal.CompletedCount >= dailyGoal.TotalCount
                    ? "Bugunun gorevleri tamam. Dilersen masallar ya da yeni bir oyun acabiliriz."
                    : $"Bugun once gunluk plani tamamlayalim. {dailyGoal.TotalCount - dailyGoal.CompletedCount} mini gorev kaldi.";
                return true;
            }
        }

        if (ContainsAny(lower, "serim kac gun", "seri kac gun", "serim ne durumda", "streak"))
        {
            answer = profile is null
                ? "Bir profil secilince serini de takip edebilirim."
                : profile.StreakDays <= 0
                    ? "Yeni bir seri baslatmaya hazirsin. Bugun kisa bir oyun yeter."
                    : $"{profile.StreakDays} gunluk bir serin var. Harika gidiyorsun.";
            return true;
        }

        if (ContainsAny(lower, "seviyem kac", "kac levelim var", "kacinci seviyedeyim", "xp"))
        {
            answer = profile is null
                ? "Bir profil secildiginde seviyeni de gosterebilirim."
                : $"{profile.LevelText}. {profile.LevelTitle} durumundasin ve sonraki seviyeye {profile.XpToNextLevel} XP kaldi.";
            return true;
        }

        if (ContainsAny(lower, "kac gorev kaldi", "gunluk gorev", "gunluk planim"))
        {
            if (dailyGoal is not null)
            {
                answer = dailyGoal.CompletedCount >= dailyGoal.TotalCount
                    ? "Gunluk planin tamamlandi."
                    : $"{dailyGoal.TotalCount - dailyGoal.CompletedCount} mini gorevin kaldi.";
                return true;
            }
        }

        if (ContainsAny(lower, "ne kadar sure kaldi", "bugun kac dakika kaldi", "limitim kaldi"))
        {
            if (profile is not null)
            {
                var settings = _profileService?.GetParentalSettings();
                var playedMinutes = _profileService?.GetTodayPlayedMinutes(profile.Id) ?? 0;
                if (settings is not null && settings.DailyTimeLimitMinutes > 0)
                {
                    var remainingMinutes = Math.Max(0, settings.DailyTimeLimitMinutes - playedMinutes);
                    answer = remainingMinutes == 0
                        ? "Bugunku sure sinirina ulasmissin."
                        : $"Bugun yaklasik {remainingMinutes} dakikan kaldi.";
                    return true;
                }

                answer = $"Bugun simdiye kadar {playedMinutes} dakika oynandi. Sure siniri acik degil.";
                return true;
            }
        }

        if (ContainsAny(lower, "sesini kapat", "sessiz", "sus"))
        {
            answer = "Sag ustteki ses dugmesinden beni sessize alabilirsin.";
            return true;
        }

        answer = "";
        return false;
    }

    private static bool TryResolveNavigation(string text, out string param, out string title)
    {
        var lower = NormalizeCommand(text);
        foreach (var entry in NavigationIntents)
        {
            if (entry.Keys.Any(key => lower.Contains(NormalizeCommand(key))))
            {
                param = entry.Route;
                title = entry.Title;
                return true;
            }
        }

        param = "";
        title = "";
        return false;
    }

    private List<NavigationIntent> GetNavigationSuggestions(string text, int maxSuggestions = 3)
    {
        var lower = NormalizeCommand(text);
        var tokens = lower
            .Split([' ', ',', '.', '!', '?'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var suggestions = new List<(NavigationIntent Intent, int Score)>();

        foreach (var intent in NavigationIntents)
        {
            var bestIntentScore = 0;
            foreach (var key in intent.Keys)
            {
                var normalizedKey = NormalizeCommand(key);
                var keyTokens = normalizedKey
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var overlap = keyTokens.Count(tokens.Contains);
                var containsBonus = normalizedKey.Contains(lower) || lower.Contains(normalizedKey) ? 2 : 0;
                var prefixBonus = keyTokens.Any(token => token.StartsWith(lower, StringComparison.Ordinal)) ? 1 : 0;
                var score = overlap + containsBonus + prefixBonus;
                bestIntentScore = Math.Max(bestIntentScore, score);
            }

            if (bestIntentScore >= 2)
                suggestions.Add((intent, bestIntentScore));
        }

        return suggestions
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Intent.Title)
            .Select(item => item.Intent)
            .DistinctBy(item => item.Route)
            .Take(maxSuggestions)
            .ToList();
    }

    private async Task ShowSuggestionsAsync(List<NavigationIntent> suggestions, string title, bool cameFromVoice)
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            SuggestionTitleLabel.Text = title;
            SuggestionChips.Children.Clear();
            _activeSuggestions = suggestions;
            _suggestionCameFromVoice = cameFromVoice;

            foreach (var suggestion in suggestions)
            {
                var chip = BuildSuggestionChip(suggestion);
                SuggestionChips.Children.Add(chip);
            }

            SuggestionPanel.IsVisible = suggestions.Count > 0;
            TipLabel.Text = title;
        });

        if (suggestions.Count > 0)
            await SpeakAsync($"{suggestions[0].Title} olabilir. Istersen alttaki onerilerden birini sec.");
    }

    private async Task ExecuteSuggestionAsync(NavigationIntent suggestion)
    {
        ClearSuggestions();
        await ReplyAsync($"{suggestion.Title} bolumunu aciyorum.");
        await Task.Delay(200);
        await NavigateByParam(suggestion.Route);
        await CloseModalAsync();
    }

    private Border BuildSuggestionChip(NavigationIntent suggestion)
    {
        var chip = new Border
        {
            BackgroundColor = Color.FromArgb("#F4F3FF"),
            Stroke = Color.FromArgb("#C7CEFF"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(14) },
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 0, 8, 8)
        };

        var tapRecognizer = new TapGestureRecognizer
        {
            CommandParameter = suggestion.Route
        };
        tapRecognizer.Tapped += OnShortcutTapped;
        chip.GestureRecognizers.Add(tapRecognizer);

        chip.Content = new HorizontalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label
                {
                    Text = GetRouteEmoji(suggestion.Route),
                    FontSize = 13,
                    VerticalOptions = LayoutOptions.Center
                },
                new Label
                {
                    Text = suggestion.Title,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#5148D4"),
                    VerticalOptions = LayoutOptions.Center
                }
            }
        };

        return chip;
    }

    private void ClearSuggestions()
    {
        _activeSuggestions.Clear();
        _suggestionCameFromVoice = false;
        if (SuggestionChips is not null)
            SuggestionChips.Children.Clear();
        if (SuggestionPanel is not null)
            SuggestionPanel.IsVisible = false;
    }

    private async Task ReplyAsync(string message, bool speak = true)
    {
        await SetTipTextAsync(message);
        if (speak)
            await SpeakAsync(message);
    }

    private Task SetTipTextAsync(string message) =>
        MainThread.InvokeOnMainThreadAsync(() => TipLabel.Text = message);

    private async Task SpeakAsync(string message)
    {
        if (_isMuted || _audioService is null || string.IsNullOrWhiteSpace(message))
            return;

        await _audioService.SpeakTextAsync(message.Replace("\"", ""));
    }

    private void SetListeningState(bool listening)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MicLabel.Text = listening ? "Dinleniyor... durdurmak icin dokun" : "Sesli komut ver";
            MicButton.BackgroundColor = listening
                ? Color.FromArgb("#FFEBEE")
                : Color.FromArgb("#EEF0FF");
        });
    }

    private void UpdateSoundToggle()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SoundLabel.Text = _isMuted ? "Sessiz" : "Ses acik";
            SoundToggleButton.BackgroundColor = _isMuted
                ? Color.FromArgb("#F3F4F6")
                : Color.FromArgb("#EEF0FF");
            SoundIcon.Opacity = _isMuted ? 0.45 : 1.0;
        });
    }

    private static string GetParamDisplayName(string param) => param switch
    {
        "animals" => "Hayvanlar",
        "fruits" => "Meyveler",
        "vegetables" => "Sebzeler",
        "colors" => "Renkler",
        "shapes" => "Sekiller",
        "vehicles" => "Araclar",
        "numbers" => "Sayilar",
        "letters" => "Harfler",
        "emotions" => "Duygular",
        "planets" => "Gezegenler",
        "cities" => "Iller",
        "countries" => "Ulkeler",
        "professions" => "Meslekler",
        "nature" => "Doga",
        "objects" => "Esyalar",
        "opposites" => "Zitlar",
        "traffic" => "Trafik",
        "weather" => "Hava Durumu",
        "seasons" => "Mevsimler",
        "__games" => "Oyunlar",
        "__dailygoal" => "Gunluk Gorevler",
        "__progress" => "Ilerleme",
        "__achievements" => "Basarimlar",
        "__tales" => "Masallar",
        "__topics" => "Konular",
        "__home" => "Ana sayfa",
        "__parental" => "Ayarlar",
        "__songs" => "Sarkilar",
        "__adventure" => "Macera",
        "__back" => "Geri",
        "__connectdots" => "Nokta Birlestir",
        "__drawing" => "Cizim",
        "__coloring" => "Boyama",
        "__shapecoloring" => "Sekil Boyama",
        "__math" => "Matematik",
        "__matching" => "Eslestirme",
        "__memory" => "Hafiza",
        "__sound" => "Sesli Tahmin",
        _ => string.IsNullOrWhiteSpace(param) ? "Ilgili bolum" : param
    };

    private static string GetRouteEmoji(string route) => route switch
    {
        "animals" => "\U0001F43E",
        "fruits" => "\U0001F34E",
        "vegetables" => "\U0001F955",
        "colors" => "\U0001F3A8",
        "shapes" => "\U0001F539",
        "vehicles" => "\U0001F697",
        "numbers" => "\U0001F522",
        "letters" => "\U0001F524",
        "emotions" => "\U0001F60A",
        "planets" => "\U0001FA90",
        "cities" => "\U0001F3D9",
        "countries" => "\U0001F30D",
        "professions" => "\U0001F4BC",
        "nature" => "\U0001F33F",
        "objects" => "\U0001F392",
        "opposites" => "\u2194",
        "traffic" => "\U0001F6A6",
        "weather" => "\u2600",
        "seasons" => "\U0001F338",
        "__games" => "\U0001F3AE",
        "__dailygoal" => "\U0001F3AF",
        "__progress" => "\U0001F4CA",
        "__achievements" => "\U0001F3C6",
        "__tales" => "\U0001F4D6",
        "__topics" => "\U0001F4DA",
        "__songs" => "\U0001F3B5",
        "__parental" => "\u2699",
        "__home" => "\U0001F3E0",
        "__math" => "\U0001F522",
        "__matching" => "\U0001F9E9",
        "__memory" => "\U0001F9E0",
        "__sound" => "\U0001F50A",
        _ => "\u2728"
    };

    private static bool ContainsAny(string source, params string[] keys) =>
        keys.Any(key => source.Contains(NormalizeCommand(key)));

    private static string NormalizeCommand(string text) =>
        VoiceCommandService.NormalizeCommand(text);

    private sealed record NavigationIntent(string[] Keys, string Route, string Title);
}

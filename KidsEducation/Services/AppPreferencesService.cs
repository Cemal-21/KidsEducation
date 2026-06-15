namespace KidsEducation.Services;

public class AppPreferencesService
{
    private const string PinKey = "parental_pin";
    public bool HasPin => Preferences.Default.ContainsKey(PinKey);
    public string? GetPin() => Preferences.Default.Get<string?>(PinKey, null);
    public void SetPin(string pin) => Preferences.Default.Set(PinKey, pin);
    public void ClearPin() => Preferences.Default.Remove(PinKey);

    private const string VoiceGenderKey = "app_voice_gender";
    private const string ThemePreferenceKey = "app_theme_preference";
    private const string ColorThemeKey = "app_color_theme";
    private const string SpeechRateKey = "app_speech_rate";
    private const string MasterVolumeKey = "app_master_volume";
    private const string EffectsEnabledKey = "app_effects_enabled";
    private const string MusicEnabledKey = "app_music_enabled";
    private const string MusicVolumeKey = "app_music_volume";

    public string VoiceGender
    {
        get => Preferences.Get(VoiceGenderKey, "female");
        set => Preferences.Set(VoiceGenderKey, value);
    }

    public string ThemePreference
    {
        get => Preferences.Get(ThemePreferenceKey, "system");
        set
        {
            Preferences.Set(ThemePreferenceKey, value);
            ApplyTheme();
        }
    }

    public double MasterVolume
    {
        get => Preferences.Get(MasterVolumeKey, 0.85);
        set => Preferences.Set(MasterVolumeKey, Math.Clamp(value, 0, 1));
    }

    public bool EffectsEnabled
    {
        get => Preferences.Get(EffectsEnabledKey, true);
        set => Preferences.Set(EffectsEnabledKey, value);
    }

    public bool MusicEnabled
    {
        get => Preferences.Get(MusicEnabledKey, true);
        set => Preferences.Set(MusicEnabledKey, value);
    }

    public double MusicVolume
    {
        get => Preferences.Get(MusicVolumeKey, 0.3);
        set => Preferences.Set(MusicVolumeKey, Math.Clamp(value, 0, 1));
    }

    public string ColorTheme
    {
        get => Preferences.Get(ColorThemeKey, "purple");
        set
        {
            Preferences.Set(ColorThemeKey, value);
            ApplyColorTheme();
        }
    }

    public double SpeechRate
    {
        get => Preferences.Get(SpeechRateKey, 1.0);
        set => Preferences.Set(SpeechRateKey, Math.Clamp(value, 0.5, 1.5));
    }

    public bool IsMaleVoice => VoiceGender == "male";

    public static IReadOnlyList<(string Id, string Label, string Emoji, string Primary, string PrimaryLight)> ColorThemes { get; } =
    [
        ("purple", "Mor",     "🟣", "#5148D4", "#6E64F7"),
        ("green",  "Yeşil",   "🟢", "#16A34A", "#22C55E"),
        ("orange", "Turuncu", "🟠", "#EA580C", "#F97316"),
        ("pink",   "Pembe",   "🩷", "#DB2777", "#EC4899"),
    ];

    public void ApplyColorTheme()
    {
        var theme = ColorThemes.FirstOrDefault(t => t.Id == ColorTheme);
        if (theme == default) theme = ColorThemes[0];

        if (Application.Current?.Resources is not { } res) return;
        res["BrandPurple"]      = Color.FromArgb(theme.Primary);
        res["BrandPurpleLight"] = Color.FromArgb(theme.PrimaryLight);
        res["Primary"]          = Color.FromArgb(theme.PrimaryLight);
        res["PrimaryBrush"]     = new SolidColorBrush(Color.FromArgb(theme.PrimaryLight));
    }

    public void ApplyTheme()
    {
        if (Application.Current is null)
            return;

        Application.Current.UserAppTheme = ThemePreference switch
        {
            "light" => AppTheme.Light,
            "dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }

    // ── AI Koç API Anahtarı ─────────────────────────────────
    private const string AnthropicApiKeyKey = "anthropic_api_key";

    public string AnthropicApiKey
    {
        get => Preferences.Get(AnthropicApiKeyKey, string.Empty);
        set => Preferences.Set(AnthropicApiKeyKey, value);
    }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(AnthropicApiKey);

    // ── Widget Veri Kaydetme ────────────────────────────────
    public static void SaveStreakForWidget(int streakDays)
    {
        Preferences.Default.Set("streak_days", streakDays);
        Preferences.Default.Set("streak_motivation", streakDays switch
        {
            0 => "Başla! 🚀",
            1 => "İlk gün! ⭐",
            < 7 => "Devam et! 💪",
            < 30 => "Harika! 🔥",
            _ => "Efsane! 🏆"
        });
#if ANDROID
        var ctx = global::Android.App.Application.Context;
        KidsEducation.Platforms.Android.StreakWidget.NotifyUpdate(ctx);
#endif
    }

    public static void SaveDailyGoalForWidget(int doneCount, int totalCount = 5)
    {
        Preferences.Default.Set("daily_goal_done", doneCount);
        Preferences.Default.Set("daily_goal_total", totalCount);
#if ANDROID
        var ctx = global::Android.App.Application.Context;
        KidsEducation.Platforms.Android.DailyGoalWidget.NotifyUpdate(ctx);
#endif
    }
}

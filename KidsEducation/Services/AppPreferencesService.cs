namespace KidsEducation.Services;

public class AppPreferencesService
{
    private const string VoiceGenderKey = "app_voice_gender";
    private const string ThemePreferenceKey = "app_theme_preference";
    private const string MasterVolumeKey = "app_master_volume";
    private const string EffectsEnabledKey = "app_effects_enabled";

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

    public bool IsMaleVoice => VoiceGender == "male";

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
}

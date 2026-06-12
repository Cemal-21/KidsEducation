using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Settings;

public partial class PreferencesViewModel : ObservableObject
{
    private readonly AppPreferencesService _preferences;
    private readonly AudioService _audioService;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private string _voiceGender = "female";
    [ObservableProperty] private string _themePreference = "system";
    [ObservableProperty] private double _masterVolume = 0.85;
    [ObservableProperty] private bool _effectsEnabled = true;

    public PreferencesViewModel(
        AppPreferencesService preferences,
        AudioService audioService,
        NavigationService navigationService)
    {
        _preferences = preferences;
        _audioService = audioService;
        _navigationService = navigationService;
    }

    public string VolumeText => $"{Math.Round(MasterVolume * 100):0}%";

    public bool IsFemaleVoice => VoiceGender == "female";
    public bool IsMaleVoice => VoiceGender == "male";
    public bool IsSystemTheme => ThemePreference == "system";
    public bool IsLightTheme => ThemePreference == "light";
    public bool IsDarkTheme => ThemePreference == "dark";

    public Color FemaleVoiceBackground => IsFemaleVoice ? Color.FromArgb(IsDarkActive ? "#342F70" : "#EDEBFF") : DefaultCardColor;
    public Color MaleVoiceBackground => IsMaleVoice ? Color.FromArgb(IsDarkActive ? "#20465A" : "#E6F8FF") : DefaultCardColor;
    public Color SystemThemeBackground => IsSystemTheme ? Color.FromArgb(IsDarkActive ? "#342F70" : "#EDEBFF") : DefaultCardColor;
    public Color LightThemeBackground => IsLightTheme ? Color.FromArgb(IsDarkActive ? "#54441E" : "#FFF4D6") : DefaultCardColor;
    public Color DarkThemeBackground => IsDarkTheme ? Color.FromArgb(IsDarkActive ? "#313A4D" : "#E8ECF5") : DefaultCardColor;

    private static bool IsDarkActive => Application.Current?.RequestedTheme == AppTheme.Dark;
    private static Color DefaultCardColor => Color.FromArgb(IsDarkActive ? "#172033" : "#FFFFFF");

    [RelayCommand]
    public void Initialize()
    {
        VoiceGender = _preferences.VoiceGender;
        ThemePreference = _preferences.ThemePreference;
        MasterVolume = _preferences.MasterVolume;
        EffectsEnabled = _preferences.EffectsEnabled;
        NotifyComputed();
    }

    partial void OnVoiceGenderChanged(string value)
    {
        _preferences.VoiceGender = value;
        NotifyComputed();
    }

    partial void OnThemePreferenceChanged(string value)
    {
        _preferences.ThemePreference = value;
        NotifyComputed();
    }

    partial void OnMasterVolumeChanged(double value)
    {
        _preferences.MasterVolume = value;
        OnPropertyChanged(nameof(VolumeText));
    }

    partial void OnEffectsEnabledChanged(bool value)
    {
        _preferences.EffectsEnabled = value;
    }

    [RelayCommand]
    public void SetVoice(string voiceGender) => VoiceGender = voiceGender;

    [RelayCommand]
    public void SetTheme(string themePreference) => ThemePreference = themePreference;

    [RelayCommand]
    public async Task PreviewVoiceAsync()
    {
        await _audioService.SpeakUIAsync("aferin", IsMaleVoice);
    }

    [RelayCommand]
    public async Task PreviewEffectAsync()
    {
        await _audioService.PlayCorrectAsync();
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        _audioService.StopSpeech();
        await _navigationService.GoBackOneAsync();
    }

    private void NotifyComputed()
    {
        OnPropertyChanged(nameof(IsFemaleVoice));
        OnPropertyChanged(nameof(IsMaleVoice));
        OnPropertyChanged(nameof(IsSystemTheme));
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(FemaleVoiceBackground));
        OnPropertyChanged(nameof(MaleVoiceBackground));
        OnPropertyChanged(nameof(SystemThemeBackground));
        OnPropertyChanged(nameof(LightThemeBackground));
        OnPropertyChanged(nameof(DarkThemeBackground));
    }
}

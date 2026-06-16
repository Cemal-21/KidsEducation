using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Enums;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Home;

public partial class ProfileSelectionViewModel : ObservableObject
{
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;
    private readonly AudioService _audioService;

    [ObservableProperty] private List<ChildProfile> _profiles = new();
    public bool HasProfiles => Profiles.Count > 0;

    partial void OnProfilesChanged(List<ChildProfile> value)
    {
        OnPropertyChanged(nameof(HasProfiles));
        OnPropertyChanged(nameof(ProfileCountText));
        UpdatePreviewState();
    }
    [ObservableProperty] private string _newProfileName = string.Empty;
    [ObservableProperty] private AgeGroup _selectedAgeGroup = AgeGroup.Toddler;
    [ObservableProperty] private string _selectedAvatarEmoji = "🐰";
    [ObservableProperty] private string _validationMessage = string.Empty;

    public string ProfileCountText => Profiles.Count == 0
        ? "Ilk profili olustur"
        : $"{Profiles.Count} hazir profil var";

    public string ProfilePreviewName => string.IsNullOrWhiteSpace(NewProfileName)
        ? "Yeni Arkadas"
        : NewProfileName.Trim();

    public string SelectedAgeGroupName => AgeGroupOptions.First(o => o.AgeGroup == SelectedAgeGroup).Name;
    public string SelectedAgeGroupRange => AgeGroupOptions.First(o => o.AgeGroup == SelectedAgeGroup).AgeRange;
    public string SelectedAgeGroupHint => SelectedAgeGroup switch
    {
        AgeGroup.Toddler => "Daha buyuk gorseller, daha sakin tempo ve 2 secenekli oyunlar.",
        AgeGroup.Explorer => "Daha fazla kesif, 4 secenekli mini oyunlar ve hizli tekrarlar.",
        _ => "Biraz daha zorlayici etkinlikler, sureli bolumler ve daha akilli ipuclari."
    };

    public string ProfileReadinessText => string.IsNullOrWhiteSpace(NewProfileName)
        ? "Ismi ekleyince profil tamamen hazir gorunecek."
        : $"{ProfilePreviewName} icin {SelectedAgeGroupRange} akisi secili.";

    public bool CanCreateProfile => IsNameValid(NewProfileName) && !HasDuplicateName(NewProfileName);

    public List<AgeGroupOption> AgeGroupOptions { get; } = new()
    {
        new AgeGroupOption(AgeGroup.Toddler,    "🐰", "Minikler",   "3-5 yaş"),
        new AgeGroupOption(AgeGroup.Explorer,   "🦊", "Keşifçiler", "5-7 yaş"),
        new AgeGroupOption(AgeGroup.Adventurer, "🦉", "Kaşifler",   "7-9 yaş"),
    };

    public ProfileSelectionViewModel(
        ProfileService profileService,
        NavigationService navigationService,
        AudioService audioService)
    {
        _profileService = profileService;
        _navigationService = navigationService;
        _audioService = audioService;
    }

    [RelayCommand]
    public void LoadProfiles()
    {
        Profiles = _profileService.GetAllProfiles();
        UpdatePreviewState();
    }

    [RelayCommand]
    public async Task SelectProfileAsync(ChildProfile profile)
    {
        _profileService.SetActiveProfile(profile.Id);
        await StartBackgroundMusicSafelyAsync();
        await _navigationService.GoToHomeAsync();
    }

    [RelayCommand]
    public void SelectAgeGroup(string ageGroupStr)
    {
        if (int.TryParse(ageGroupStr, out int value))
            SelectedAgeGroup = (AgeGroup)value;
    }

    [RelayCommand]
    public async Task CreateProfileAsync()
    {
        var trimmedName = NewProfileName.Trim();
        if (!IsNameValid(trimmedName))
        {
            ValidationMessage = "Profil adi en az 2 harf olmali.";
            UpdatePreviewState();
            return;
        }

        if (HasDuplicateName(trimmedName))
        {
            ValidationMessage = "Bu isimde bir profil zaten var. Biraz farkli bir isim deneyelim.";
            UpdatePreviewState();
            return;
        }

        var profile = new ChildProfile
        {
            Name = trimmedName,
            AgeGroup = SelectedAgeGroup,
            AvatarEmoji = SelectedAvatarEmoji
        };

        _profileService.SaveProfile(profile);
        _profileService.SetActiveProfile(profile.Id);
        await StartBackgroundMusicSafelyAsync();

        NewProfileName = string.Empty;
        ValidationMessage = string.Empty;
        await _navigationService.GoToHomeAsync();
    }

    [RelayCommand]
    public void DeleteProfile(ChildProfile profile)
    {
        _profileService.DeleteProfile(profile.Id);
        LoadProfiles();
    }

    private async Task StartBackgroundMusicSafelyAsync()
    {
        try
        {
            await _audioService.StartBackgroundMusicAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfileSelection] Müzik başlatılamadı: {ex.Message}");
        }
    }

    partial void OnNewProfileNameChanged(string value) => UpdatePreviewState();
    partial void OnSelectedAgeGroupChanged(AgeGroup value) => UpdatePreviewState();
    partial void OnSelectedAvatarEmojiChanged(string value) => UpdatePreviewState();

    private void UpdatePreviewState()
    {
        if (string.IsNullOrWhiteSpace(NewProfileName))
            ValidationMessage = string.Empty;
        else if (!IsNameValid(NewProfileName))
            ValidationMessage = "Profil adi en az 2 harf olmali.";
        else if (HasDuplicateName(NewProfileName))
            ValidationMessage = "Ayni isim zaten kullaniliyor.";
        else
            ValidationMessage = "Hazir. Bu profil olusturulabilir.";

        OnPropertyChanged(nameof(ProfilePreviewName));
        OnPropertyChanged(nameof(SelectedAgeGroupName));
        OnPropertyChanged(nameof(SelectedAgeGroupRange));
        OnPropertyChanged(nameof(SelectedAgeGroupHint));
        OnPropertyChanged(nameof(ProfileReadinessText));
        OnPropertyChanged(nameof(CanCreateProfile));
    }

    private bool HasDuplicateName(string name) =>
        Profiles.Any(profile => string.Equals(profile.Name?.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase));

    private static bool IsNameValid(string name) => !string.IsNullOrWhiteSpace(name) && name.Trim().Length >= 2;
}

public class AgeGroupOption
{
    public AgeGroup AgeGroup { get; }
    public string Emoji { get; }
    public string Name { get; }
    public string AgeRange { get; }

    public AgeGroupOption(AgeGroup ageGroup, string emoji, string name, string ageRange)
    {
        AgeGroup = ageGroup;
        Emoji = emoji;
        Name = name;
        AgeRange = ageRange;
    }
}

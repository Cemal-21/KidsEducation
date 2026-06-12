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

    [ObservableProperty] private List<ChildProfile> _profiles = new();
    [ObservableProperty] private string _newProfileName = string.Empty;
    [ObservableProperty] private AgeGroup _selectedAgeGroup = AgeGroup.Toddler;

    public List<AgeGroupOption> AgeGroupOptions { get; } = new()
    {
        new AgeGroupOption(AgeGroup.Toddler,    "🐰", "Minikler",   "3-5 yaş"),
        new AgeGroupOption(AgeGroup.Explorer,   "🦊", "Keşifçiler", "5-7 yaş"),
        new AgeGroupOption(AgeGroup.Adventurer, "🦉", "Kaşifler",   "7-9 yaş"),
    };

    public ProfileSelectionViewModel(
        ProfileService profileService,
        NavigationService navigationService)
    {
        _profileService = profileService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public void LoadProfiles()
    {
        Profiles = _profileService.GetAllProfiles();
    }

    [RelayCommand]
    public async Task SelectProfileAsync(ChildProfile profile)
    {
        _profileService.SetActiveProfile(profile.Id);
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
        if (string.IsNullOrWhiteSpace(NewProfileName)) return;

        var option = AgeGroupOptions.First(o => o.AgeGroup == SelectedAgeGroup);

        var profile = new ChildProfile
        {
            Name = NewProfileName.Trim(),
            AgeGroup = SelectedAgeGroup,
            AvatarEmoji = option.Emoji
        };

        _profileService.SaveProfile(profile);
        _profileService.SetActiveProfile(profile.Id);

        NewProfileName = string.Empty;
        await _navigationService.GoToHomeAsync();
    }

    [RelayCommand]
    public void DeleteProfile(ChildProfile profile)
    {
        _profileService.DeleteProfile(profile.Id);
        LoadProfiles();
    }
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
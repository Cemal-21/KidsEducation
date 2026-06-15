using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Parental;

public partial class PinEntryViewModel : ObservableObject
{
    private readonly AppPreferencesService _prefs;
    private readonly NavigationService _nav;

    [ObservableProperty] private string _pin = "";
    [ObservableProperty] private string _dot1 = "○";
    [ObservableProperty] private string _dot2 = "○";
    [ObservableProperty] private string _dot3 = "○";
    [ObservableProperty] private string _dot4 = "○";
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private bool _hasError;

    public PinEntryViewModel(AppPreferencesService prefs, NavigationService nav)
    {
        _prefs = prefs;
        _nav = nav;
    }

    [RelayCommand]
    public void PressDigit(string digit)
    {
        if (Pin.Length >= 4) return;
        Pin += digit;
        UpdateDots();
        if (Pin.Length == 4) _ = ValidateAsync();
    }

    [RelayCommand]
    public void DeleteDigit()
    {
        if (Pin.Length == 0) return;
        Pin = Pin[..^1];
        HasError = false;
        ErrorMessage = "";
        UpdateDots();
    }

    [RelayCommand]
    public Task GoBackAsync() => _nav.GoBackAsync();

    private void UpdateDots()
    {
        Dot1 = Pin.Length >= 1 ? "●" : "○";
        Dot2 = Pin.Length >= 2 ? "●" : "○";
        Dot3 = Pin.Length >= 3 ? "●" : "○";
        Dot4 = Pin.Length >= 4 ? "●" : "○";
    }

    private async Task ValidateAsync()
    {
        await Task.Delay(150); // small pause so 4th dot shows
        if (Pin == _prefs.GetPin())
        {
            Pin = "";
            UpdateDots();
            HasError = false;
            ErrorMessage = "";
            await Shell.Current.GoToAsync("//parental");
        }
        else
        {
            HasError = true;
            ErrorMessage = "Yanlış PIN. Tekrar dene.";
            await Task.Delay(600);
            Pin = "";
            UpdateDots();
            HasError = false;
            ErrorMessage = "";
        }
    }
}

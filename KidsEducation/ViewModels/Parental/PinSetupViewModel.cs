using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Parental;

public partial class PinSetupViewModel : ObservableObject
{
    private readonly AppPreferencesService _prefs;
    private readonly NavigationService _nav;

    [ObservableProperty] private string _pin = "";
    [ObservableProperty] private string _confirmPin = "";
    [ObservableProperty] private bool _isConfirmStep;
    [ObservableProperty] private string _dot1 = "○";
    [ObservableProperty] private string _dot2 = "○";
    [ObservableProperty] private string _dot3 = "○";
    [ObservableProperty] private string _dot4 = "○";
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private string _stepTitle = "Yeni PIN belirle";
    [ObservableProperty] private string _stepSubtitle = "4 haneli bir PIN girin";

    private string _firstPin = "";

    public PinSetupViewModel(AppPreferencesService prefs, NavigationService nav)
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
        if (Pin.Length == 4) _ = AdvanceAsync();
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

    private async Task AdvanceAsync()
    {
        await Task.Delay(150);
        if (!IsConfirmStep)
        {
            _firstPin = Pin;
            Pin = "";
            UpdateDots();
            IsConfirmStep = true;
            StepTitle = "PIN'i onayla";
            StepSubtitle = "Aynı PIN'i tekrar girin";
        }
        else
        {
            if (Pin == _firstPin)
            {
                _prefs.SetPin(Pin);
                Pin = "";
                UpdateDots();
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                HasError = true;
                ErrorMessage = "PIN'ler eşleşmedi. Tekrar dene.";
                await Task.Delay(700);
                Pin = "";
                _firstPin = "";
                IsConfirmStep = false;
                StepTitle = "Yeni PIN belirle";
                StepSubtitle = "4 haneli bir PIN girin";
                UpdateDots();
                HasError = false;
                ErrorMessage = "";
            }
        }
    }
}

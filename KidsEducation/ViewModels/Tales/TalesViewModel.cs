using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Tales;

public partial class TalesViewModel : ObservableObject
{
    private readonly TaleService _taleService;

    [ObservableProperty] private List<Tale> _tales = new();
    [ObservableProperty] private bool _isLoading = true;

    public TalesViewModel(TaleService taleService)
    {
        _taleService = taleService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        Tales = await _taleService.GetAllTalesAsync();
        IsLoading = false;
    }

    [RelayCommand]
    public Task OpenTaleAsync(Tale tale) =>
        Shell.Current.GoToAsync($"talereader?taleId={tale.Id}");

    [RelayCommand]
    public Task GoBackAsync() => Shell.Current.GoToAsync("..");
}

using KidsEducation.ViewModels.Game;

namespace KidsEducation.Views.Game;

public partial class MemoryGamePage : ContentPage
{
    private readonly MemoryGameViewModel _viewModel;

    public MemoryGamePage(MemoryGameViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeCommand.ExecuteAsync(null);
    }
}
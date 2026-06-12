using KidsEducation.ViewModels.Game;

namespace KidsEducation.Views.Game;

public partial class SoundGamePage : ContentPage
{
    private readonly SoundGameViewModel _viewModel;

    public SoundGamePage(SoundGameViewModel viewModel)
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

    protected override void OnDisappearing()
    {
        _viewModel.StopSound();
        base.OnDisappearing();
    }
}

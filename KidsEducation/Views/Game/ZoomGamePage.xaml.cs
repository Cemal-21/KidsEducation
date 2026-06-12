using KidsEducation.ViewModels.Game;

namespace KidsEducation.Views.Game;

public partial class ZoomGamePage : ContentPage
{
    private readonly ZoomGameViewModel _viewModel;

    public ZoomGamePage(ZoomGameViewModel viewModel)
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
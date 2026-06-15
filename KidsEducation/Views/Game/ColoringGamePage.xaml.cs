using KidsEducation.ViewModels.Game;

namespace KidsEducation.Views.Game;

public partial class ColoringGamePage : ContentPage
{
    private readonly ColoringGameViewModel _viewModel;

    public ColoringGamePage(ColoringGameViewModel viewModel)
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

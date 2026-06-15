using KidsEducation.ViewModels.Game;

namespace KidsEducation.Views.Game;

public partial class MathGamePage : ContentPage
{
    private readonly MathGameViewModel _viewModel;

    public MathGamePage(MathGameViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}

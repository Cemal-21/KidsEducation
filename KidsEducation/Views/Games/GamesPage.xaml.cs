using KidsEducation.ViewModels.Games;

namespace KidsEducation.Views.Games;

public partial class GamesPage : ContentPage
{
    private readonly GamesViewModel _viewModel;

    public GamesPage(GamesViewModel viewModel)
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

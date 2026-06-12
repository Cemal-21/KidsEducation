using KidsEducation.ViewModels.Game;

namespace KidsEducation.Views.Game;

public partial class StoryGamePage : ContentPage
{
    private readonly StoryGameViewModel _viewModel;

    public StoryGamePage(StoryGameViewModel viewModel)
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

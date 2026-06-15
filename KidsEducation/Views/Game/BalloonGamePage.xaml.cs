using KidsEducation.ViewModels.Game;

namespace KidsEducation.Views.Game;

[QueryProperty(nameof(CategoryId), "categoryId")]
public partial class BalloonGamePage : ContentPage
{
    private readonly BalloonGameViewModel _viewModel;

    public string? CategoryId
    {
        get => _viewModel.CategoryId;
        set => _viewModel.CategoryId = value ?? string.Empty;
    }

    public BalloonGamePage(BalloonGameViewModel viewModel)
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
        _viewModel.StopGame();
        base.OnDisappearing();
    }
}

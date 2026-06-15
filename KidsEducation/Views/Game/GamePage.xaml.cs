using KidsEducation.ViewModels.Game;

namespace KidsEducation.Views.Game;

[QueryProperty(nameof(CategoryId), "categoryId")]
[QueryProperty(nameof(GameTypeName), "gameType")]
public partial class GamePage : ContentPage
{
    private readonly GameViewModel _viewModel;

    public string? CategoryId
    {
        get => _viewModel.CategoryId;
        set => _viewModel.CategoryId = value ?? string.Empty;
    }

    public string? GameTypeName
    {
        get => _viewModel.GameTypeName;
        set => _viewModel.GameTypeName = value ?? string.Empty;
    }

    public GamePage(GameViewModel viewModel)
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

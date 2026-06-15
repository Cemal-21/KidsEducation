using KidsEducation.ViewModels.Game;

namespace KidsEducation.Views.Game;

[QueryProperty(nameof(CategoryId), "categoryId")]
public partial class PuzzleGamePage : ContentPage
{
    private readonly PuzzleGameViewModel _viewModel;

    public string? CategoryId { get; set; }

    public PuzzleGamePage(PuzzleGameViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeCommand.ExecuteAsync(CategoryId);
    }
}

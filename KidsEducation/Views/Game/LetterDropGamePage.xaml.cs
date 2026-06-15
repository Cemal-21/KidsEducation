using KidsEducation.ViewModels.Game;

namespace KidsEducation.Views.Game;

[QueryProperty(nameof(CategoryId), "categoryId")]
public partial class LetterDropGamePage : ContentPage
{
    private readonly LetterDropGameViewModel _viewModel;

    public string? CategoryId { get; set; }

    public LetterDropGamePage(LetterDropGameViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync(CategoryId);
    }
}

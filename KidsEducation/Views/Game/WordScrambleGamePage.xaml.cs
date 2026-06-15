using KidsEducation.ViewModels.Game;

namespace KidsEducation.Views.Game;

[QueryProperty(nameof(CategoryId), "categoryId")]
public partial class WordScrambleGamePage : ContentPage
{
    private readonly WordScrambleGameViewModel _viewModel;

    public string? CategoryId { get; set; }

    public WordScrambleGamePage(WordScrambleGameViewModel viewModel)
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

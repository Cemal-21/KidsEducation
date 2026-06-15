using KidsEducation.ViewModels.Game;

namespace KidsEducation.Views.Game;

[QueryProperty(nameof(CategoryId), "categoryId")]
public partial class QuizGamePage : ContentPage
{
    private readonly QuizGameViewModel _viewModel;

    public string? CategoryId { get; set; }

    public QuizGamePage(QuizGameViewModel viewModel)
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

using KidsEducation.ViewModels.Games;

namespace KidsEducation.Views.Games;

[QueryProperty(nameof(CategoryId), "categoryId")]
public partial class CategoryGamesPage : ContentPage
{
    private readonly CategoryGamesViewModel _viewModel;

    public string? CategoryId { get; set; }

    public CategoryGamesPage(CategoryGamesViewModel viewModel)
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

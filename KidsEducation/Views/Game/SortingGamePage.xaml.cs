using KidsEducation.ViewModels.Game;

namespace KidsEducation.Views.Game;

[QueryProperty(nameof(CategoryId), "categoryId")]
public partial class SortingGamePage : Views.AnimatedPage
{
    private readonly SortingGameViewModel _viewModel;

    public string? CategoryId { get; set; }

    public SortingGamePage(SortingGameViewModel viewModel)
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

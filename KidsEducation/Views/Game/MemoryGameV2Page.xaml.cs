using KidsEducation.ViewModels.Game;

namespace KidsEducation.Views.Game;

[QueryProperty(nameof(CategoryId), "categoryId")]
public partial class MemoryGameV2Page : Views.AnimatedPage
{
    private readonly MemoryGameV2ViewModel _viewModel;

    public string? CategoryId { get; set; }

    public MemoryGameV2Page(MemoryGameV2ViewModel viewModel)
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

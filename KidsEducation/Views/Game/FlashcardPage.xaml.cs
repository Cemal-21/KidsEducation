using KidsEducation.ViewModels.Game;

namespace KidsEducation.Views.Game;

[QueryProperty(nameof(CategoryId), "categoryId")]
public partial class FlashcardPage : ContentPage
{
    private readonly FlashcardViewModel _viewModel;

    public string? CategoryId { get; set; }

    public FlashcardPage(FlashcardViewModel viewModel)
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

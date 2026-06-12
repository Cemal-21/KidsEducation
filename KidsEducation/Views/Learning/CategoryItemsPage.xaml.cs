using KidsEducation.ViewModels.Learning;

namespace KidsEducation.Views.Learning;

public partial class CategoryItemsPage : AnimatedPage
{
    private readonly CategoryItemsViewModel _viewModel;

    public CategoryItemsPage(CategoryItemsViewModel viewModel)
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
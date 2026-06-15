using KidsEducation.ViewModels.Story;

namespace KidsEducation.Views.Story;

public partial class StoryReaderPage : Views.AnimatedPage
{
    private readonly StoryReaderViewModel _viewModel;

    public StoryReaderPage(StoryReaderViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}

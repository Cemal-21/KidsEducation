using KidsEducation.ViewModels.Songs;

namespace KidsEducation.Views.Songs;

public partial class SongsPage : AnimatedPage
{
    private readonly SongsViewModel _viewModel;

    public SongsPage(SongsViewModel viewModel)
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

using KidsEducation.ViewModels.Songs;

namespace KidsEducation.Views.Songs;

public partial class SongDetailPage : ContentPage
{
    private readonly SongDetailViewModel _viewModel;

    public SongDetailPage(SongDetailViewModel viewModel)
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

    protected override void OnDisappearing()
    {
        _viewModel.StopSong();
        base.OnDisappearing();
    }
}

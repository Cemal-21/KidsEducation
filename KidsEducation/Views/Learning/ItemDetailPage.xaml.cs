using KidsEducation.ViewModels.Learning;

namespace KidsEducation.Views.Learning;

public partial class ItemDetailPage : AnimatedPage
{
    private readonly ItemDetailViewModel _viewModel;

    public ItemDetailPage(ItemDetailViewModel viewModel)
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
        _viewModel.StopSpeech();
        base.OnDisappearing();
    }
}

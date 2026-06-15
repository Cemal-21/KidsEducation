using KidsEducation.ViewModels.Adventure;

namespace KidsEducation.Views.Adventure;

public partial class AdventureMapPage : AnimatedPage
{
    private readonly AdventureMapViewModel _viewModel;

    public AdventureMapPage(AdventureMapViewModel viewModel)
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

using KidsEducation.ViewModels.Home;

namespace KidsEducation.Views.Home;

public partial class ProfileSelectionPage : ContentPage
{
    private readonly ProfileSelectionViewModel _viewModel;

    public ProfileSelectionPage(ProfileSelectionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadProfilesCommand.Execute(null);
    }
}
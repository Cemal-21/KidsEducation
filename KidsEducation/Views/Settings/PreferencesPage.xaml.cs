using KidsEducation.ViewModels.Settings;

namespace KidsEducation.Views.Settings;

public partial class PreferencesPage : AnimatedPage
{
    private readonly PreferencesViewModel _viewModel;

    public PreferencesPage(PreferencesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.InitializeCommand.Execute(null);
    }

}

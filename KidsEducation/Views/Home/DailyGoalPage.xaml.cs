using KidsEducation.ViewModels.Home;

namespace KidsEducation.Views.Home;

public partial class DailyGoalPage : ContentPage
{
    private readonly DailyGoalViewModel _viewModel;

    public DailyGoalPage(DailyGoalViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Initialize();
    }
}

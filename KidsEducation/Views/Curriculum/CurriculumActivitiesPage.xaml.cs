using KidsEducation.ViewModels.Curriculum;

namespace KidsEducation.Views.Curriculum;

public partial class CurriculumActivitiesPage : ContentPage
{
    private readonly CurriculumActivitiesViewModel _viewModel;

    public CurriculumActivitiesPage(CurriculumActivitiesViewModel viewModel)
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

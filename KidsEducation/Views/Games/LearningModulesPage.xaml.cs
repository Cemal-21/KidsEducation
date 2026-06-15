using KidsEducation.ViewModels.Games;

namespace KidsEducation.Views.Games;

public partial class LearningModulesPage : ContentPage
{
    private readonly LearningModulesViewModel _viewModel;

    public LearningModulesPage(LearningModulesViewModel viewModel)
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

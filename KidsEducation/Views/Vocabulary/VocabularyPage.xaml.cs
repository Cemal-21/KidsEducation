using KidsEducation.ViewModels.Vocabulary;

namespace KidsEducation.Views.Vocabulary;

public partial class VocabularyPage : Views.AnimatedPage
{
    private readonly VocabularyViewModel _viewModel;

    public VocabularyPage(VocabularyViewModel viewModel)
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

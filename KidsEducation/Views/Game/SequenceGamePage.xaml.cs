using KidsEducation.ViewModels.Game;

namespace KidsEducation.Views.Game;

public partial class SequenceGamePage : ContentPage
{
    private readonly SequenceGameViewModel _viewModel;

    public SequenceGamePage(SequenceGameViewModel viewModel)
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

using KidsEducation.ViewModels.Game;

namespace KidsEducation.Views.Game;

public partial class MultiplayerPage : ContentPage
{
    private readonly MultiplayerViewModel _vm;

    public MultiplayerPage(MultiplayerViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MultiplayerViewModel.PageState))
                UpdatePanels();
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdatePanels();
    }

    private void UpdatePanels()
    {
        var state = _vm.PageState;

        RolePanel.IsVisible       = state == MultiplayerPageState.RoleSelect;
        HostWaitingPanel.IsVisible = state == MultiplayerPageState.HostWaiting
                                  || state == MultiplayerPageState.HostQuestion;
        ChildPanel.IsVisible      = state == MultiplayerPageState.ChildWaiting
                                  || state == MultiplayerPageState.ChildQuestion
                                  || state == MultiplayerPageState.ChildAnswered;
        GameOverPanel.IsVisible   = state == MultiplayerPageState.GameOver;
    }
}

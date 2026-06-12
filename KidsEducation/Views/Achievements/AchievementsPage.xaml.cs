using KidsEducation.ViewModels.Achievements;

namespace KidsEducation.Views.Achievements;

public partial class AchievementsPage : AnimatedPage
{
    private readonly AchievementsViewModel _vm;

    public AchievementsPage(AchievementsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeCommand.ExecuteAsync(null);
    }
}

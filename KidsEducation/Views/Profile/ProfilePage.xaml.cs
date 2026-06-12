using KidsEducation.ViewModels.Profile;


namespace KidsEducation.Views.Profile;

public partial class ProfilePage : AnimatedPage
{
    private readonly ProfileViewModel _vm;

    public ProfilePage(ProfileViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        vm.InitializeCommand.Execute(null);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
    }
}

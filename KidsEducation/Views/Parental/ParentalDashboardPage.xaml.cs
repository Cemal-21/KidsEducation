using KidsEducation.ViewModels.Parental;

namespace KidsEducation.Views.Parental;

public partial class ParentalDashboardPage : KidsEducation.Views.AnimatedPage
{
    public ParentalDashboardPage(ParentalDashboardViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ParentalDashboardViewModel vm)
            await vm.InitializeCommand.ExecuteAsync(null);
    }
}

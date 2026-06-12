using KidsEducation.ViewModels.Parental;

namespace KidsEducation.Views.Parental;

public partial class ParentalPage : AnimatedPage
{
    private readonly ParentalViewModel _vm;

    public ParentalPage(ParentalViewModel vm)
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

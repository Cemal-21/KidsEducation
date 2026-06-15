using KidsEducation.ViewModels.Parental;
namespace KidsEducation.Views.Parental;
public partial class PinSetupPage : ContentPage
{
    public PinSetupPage(PinSetupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

using KidsEducation.ViewModels.Parental;
namespace KidsEducation.Views.Parental;
public partial class PinEntryPage : ContentPage
{
    public PinEntryPage(PinEntryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

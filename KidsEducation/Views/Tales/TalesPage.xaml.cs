using KidsEducation.ViewModels.Tales;

namespace KidsEducation.Views.Tales;

public partial class TalesPage : ContentPage
{
    private readonly TalesViewModel _vm;

    public TalesPage(TalesViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.InitializeCommand.Execute(null);
    }
}

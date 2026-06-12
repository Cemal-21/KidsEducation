using KidsEducation.ViewModels.Progress;


namespace KidsEducation.Views.Progress;

public partial class ProgressPage : AnimatedPage
{
    private readonly ProgressViewModel _vm;

    public ProgressPage(ProgressViewModel vm)
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

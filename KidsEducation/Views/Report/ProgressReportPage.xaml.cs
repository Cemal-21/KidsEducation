using KidsEducation.ViewModels.Report;

namespace KidsEducation.Views.Report;

public partial class ProgressReportPage : ContentPage
{
    private readonly ProgressReportViewModel _vm;

    public ProgressReportPage(ProgressReportViewModel vm)
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

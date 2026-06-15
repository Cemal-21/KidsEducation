using KidsEducation.ViewModels.Pronunciation;

namespace KidsEducation.Views.Pronunciation;

[QueryProperty(nameof(CategoryId), "categoryId")]
public partial class PronunciationGamePage : ContentPage
{
    private readonly PronunciationGameViewModel _vm;

    public string CategoryId
    {
        set => _vm.CategoryId = value;
    }

    public PronunciationGamePage(PronunciationGameViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeCommand.ExecuteAsync(_vm.CategoryId);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.GoBackCommand.Execute(null);
    }
}

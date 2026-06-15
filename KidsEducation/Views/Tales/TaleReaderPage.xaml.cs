using KidsEducation.ViewModels.Tales;

namespace KidsEducation.Views.Tales;

public partial class TaleReaderPage : ContentPage
{
    private readonly TaleReaderViewModel _vm;

    public TaleReaderPage(TaleReaderViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TaleReaderViewModel.IsPlaying))
                UpdateAudioButton(vm.IsPlaying);
        };
    }

    private void UpdateAudioButton(bool isPlaying)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            AudioIcon.Text  = isPlaying ? "🔊" : "🎧";
            AudioLabel.Text = isPlaying ? "Oynatılıyor..." : "Dinle";
        });
    }
}

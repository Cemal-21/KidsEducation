using KidsEducation.ViewModels.Result;

namespace KidsEducation.Views.Result;

public partial class ResultPage : AnimatedPage
{
    private readonly ResultViewModel _vm;

    public ResultPage(ResultViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Kısa gecikme — sayfa tam yüklendikten sonra göster
        await Task.Delay(400);
        await ShowCelebrationAsync();
    }

    private async Task ShowCelebrationAsync()
    {
        // Yıldız sayısına göre başlık ve emoji belirle
        var (title, subtitle, emoji) = _vm.Stars switch
        {
            3 => ("Mükemmel! 🏆", $"{_vm.Stars} yıldız kazandın!", "🥇"),
            2 => ("Çok İyi! 🎉", $"{_vm.Stars} yıldız kazandın!", "🎊"),
            1 => ("Aferin! 👏", "1 yıldız kazandın, devam et!", "⭐"),
            _ => ("Denemeye Devam!", "Bir daha dene!", "💪")
        };

        await Celebration.ShowAsync(
            title: title,
            subtitle: subtitle,
            emoji: emoji,
            stars: _vm.Stars,
            autoDismissSeconds: 0); // Manuel kapat
    }

    private void OnCelebrationDismissed(object? sender, EventArgs e)
    {
        // Overlay kapandığında yapılacaklar (isteğe bağlı)
    }
}

using Microsoft.Maui.Layouts;

namespace KidsEducation.Views.Controls;

public partial class CelebrationOverlay : ContentView
{
    private readonly Random _rng = new();
    private bool _isAnimating;

    // Konfeti renkleri
    private static readonly string[] ConfettiColors =
    {
        "#FF6B9D", "#FFD66B", "#6C62F5", "#34C759",
        "#FF9500", "#00B894", "#FF3B30", "#007AFF"
    };

    // Konfeti emojileri
    private static readonly string[] ConfettiEmojis =
    {
        "⭐", "🎉", "✨", "🌟", "💫", "🎊"
    };

    public CelebrationOverlay()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Kutlamayı başlatır.
    /// </summary>
    /// <param name="title">Başlık metni (örn. "Harika!")</param>
    /// <param name="subtitle">Alt başlık (örn. "3 yıldız kazandın!")</param>
    /// <param name="emoji">Büyük emoji (örn. "🏆")</param>
    /// <param name="stars">Gösterilecek yıldız sayısı (0-3)</param>
    /// <param name="autoDismissSeconds">Otomatik kapanma (0 = kapatma)</param>
    public async Task ShowAsync(
        string title = "Harika!",
        string subtitle = "Tebrikler!",
        string emoji = "🎉",
        int stars = 3,
        int autoDismissSeconds = 0)
    {
        if (_isAnimating) return;
        _isAnimating = true;

        // İçeriği ayarla
        CelebrationTitle.Text = title;
        CelebrationSubtitle.Text = subtitle;
        CelebrationEmoji.Text = emoji;

        // Yıldızları sıfırla
        Star1.Opacity = Star2.Opacity = Star3.Opacity = 0;
        Star1.Scale = Star2.Scale = Star3.Scale = 0.5;

        // Görünür yap
        IsVisible = true;
        Opacity = 0;
        await this.FadeTo(1, 300);

        // Konfeti başlat
        _ = Task.Run(() => SpawnConfettiAsync());

        // Yıldızları sırayla göster
        await Task.Delay(200);
        if (stars >= 1) await AnimateStarAsync(Star1);
        if (stars >= 2) await AnimateStarAsync(Star2);
        if (stars >= 3) await AnimateStarAsync(Star3);

        // Otomatik kapat
        if (autoDismissSeconds > 0)
        {
            await Task.Delay(autoDismissSeconds * 1000);
            await DismissAsync();
        }

        _isAnimating = false;
    }

    public async Task DismissAsync()
    {
        await this.FadeTo(0, 250);
        IsVisible = false;
        ConfettiCanvas.Children.Clear();
    }

    private async Task AnimateStarAsync(Label star)
    {
        star.Opacity = 1;
        await star.ScaleTo(1.3, 150, Easing.SpringOut);
        await star.ScaleTo(1.0, 100);
    }

    private async Task SpawnConfettiAsync()
    {
        var width = DeviceDisplay.MainDisplayInfo.Width /
                    DeviceDisplay.MainDisplayInfo.Density;

        for (int i = 0; i < 30; i++)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var label = new Label
                {
                    Text = ConfettiEmojis[_rng.Next(ConfettiEmojis.Length)],
                    FontSize = _rng.Next(16, 30),
                    Opacity = 1,
                };

                double x = _rng.NextDouble() * width;
                AbsoluteLayout.SetLayoutBounds(label, new Rect(x, -40, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
                AbsoluteLayout.SetLayoutFlags(label, AbsoluteLayoutFlags.None);
                ConfettiCanvas.Children.Add(label);

                // Düşme animasyonu
                var duration = _rng.Next(1200, 2400);
                var targetY = DeviceDisplay.MainDisplayInfo.Height /
                               DeviceDisplay.MainDisplayInfo.Density + 60;

                _ = label.TranslateTo(
                    _rng.Next(-60, 60),
                    targetY,
                    (uint)duration,
                    Easing.SinIn);

                _ = label.RotateTo(
                    _rng.Next(-360, 360),
                    (uint)duration);

                _ = label.FadeTo(0, (uint)(duration * 0.8));
            });

            await Task.Delay(_rng.Next(40, 120));
        }
    }

    private async void OnDismissTapped(object sender, TappedEventArgs e)
    {
        await DismissAsync();
        Dismissed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Kullanıcı "Devam Et"e bastığında tetiklenir.
    /// </summary>
    public event EventHandler? Dismissed;
}

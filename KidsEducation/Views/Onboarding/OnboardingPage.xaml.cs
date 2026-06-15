namespace KidsEducation.Views.Onboarding;

public partial class OnboardingPage : ContentPage
{
    private int _currentIndex;

    public List<OnboardingSlide> Slides { get; } = new()
    {
        new OnboardingSlide
        {
            Emoji = "👋",
            Title = "Birlikte Öğrenelim!",
            Subtitle = "Eğlenceli oyunlar ve renkli içeriklerle öğrenmek hiç bu kadar keyifli olmamıştı.",
            Features = new()
            {
                "Türkçe'ye %100 uyumlu içerik",
                "3-9 yaş için gelişime uygun oyunlar",
                "Ebeveyn takip paneli ile güvenli kullanım"
            }
        },
        new OnboardingSlide
        {
            Emoji = "🎮",
            Title = "11 Farklı Oyun",
            Subtitle = "Her oyun farklı bir beceriyi destekler. Kelimeler, sayılar, hafıza ve daha fazlası!",
            Features = new()
            {
                "Kelime eşleştirme ve harf oyunları",
                "Matematik ve mantık soruları",
                "Hafıza kartları ve puzzle'lar"
            }
        },
        new OnboardingSlide
        {
            Emoji = "⭐",
            Title = "İlerle ve Kazan!",
            Subtitle = "Yıldız topla, seri kır, rozetler kazan. Öğrenmek bir maceraya dönüşüyor!",
            Features = new()
            {
                "Her oyun sonunda yıldız ve XP kazan",
                "Günlük hedefleri tamamla",
                "Kendi profilini oluştur, ilerlemeyi takip et"
            }
        },
        new OnboardingSlide
        {
            Emoji = "🦉",
            Title = "Akıllı Asistan & Aile",
            Subtitle = "Baykuş asistan seni yönlendirir. Sesli komut ver, ailen ile yarış!",
            Features = new()
            {
                "🎤 'Hayvanlar' de — asistan seni götürür",
                "👨‍👧 Aynı Wi-Fi'de ebeveyn ile yarış",
                "✏️ Çizim çiz, AI şeklini tanısın"
            }
        }
    };

    public OnboardingPage()
    {
        InitializeComponent();
        BindingContext = this;
        Indicators.ItemsSource = Slides;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Onboarding daha önce tamamlandıysa direkt geç
        if (Preferences.Get("onboarding_done", false))
            Shell.Current.GoToAsync("//profileselection");
    }

    private void OnSlideChanged(object? sender, CurrentItemChangedEventArgs e)
    {
        _currentIndex = Slides.IndexOf((OnboardingSlide)e.CurrentItem);
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        bool isLast = _currentIndex == Slides.Count - 1;
        NextLabel.Text = isLast ? "🚀 Başla!" : "İlerle →";
        SkipBtn.IsVisible = !isLast;
    }

    private void OnNextTapped(object? sender, TappedEventArgs e)
    {
        if (_currentIndex < Slides.Count - 1)
        {
            Carousel.ScrollTo(_currentIndex + 1, animate: true);
        }
        else
        {
            CompleteOnboarding();
        }
    }

    private void OnSkipClicked(object? sender, EventArgs e) => CompleteOnboarding();

    private static async void CompleteOnboarding()
    {
        Preferences.Set("onboarding_done", true);

        // Bildirim izni iste ve günlük hatırlatma kur
        try
        {
            var notif = IPlatformApplication.Current?.Services
                .GetService<KidsEducation.Services.NotificationService>();
            if (notif is not null)
            {
                await notif.RequestPermissionAsync();
                await notif.ScheduleDailyReminderAsync(TimeSpan.FromHours(18)); // Saat 18:00
            }
        }
        catch { }

        await Shell.Current.GoToAsync("//profileselection");
    }
}

public class OnboardingSlide
{
    public string Emoji { get; set; } = "";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public List<string> Features { get; set; } = new();
}

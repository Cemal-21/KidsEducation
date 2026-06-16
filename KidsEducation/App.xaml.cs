namespace KidsEducation;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        var prefs = new Services.AppPreferencesService();
        prefs.ApplyTheme();
        prefs.ApplyColorTheme();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var shell = new AppShell();
        _ = EnsureNotificationsAsync();

        // İlk açılış değilse onboarding'i atla
        if (Preferences.Default.Get("onboarding_done", false))
            shell.GoToAsync("//profileselection");

        return new Window(shell);
    }

    private static async Task EnsureNotificationsAsync()
    {
        try
        {
            var notificationService = IPlatformApplication.Current?.Services
                .GetService<Services.NotificationService>();
            if (notificationService is not null)
                await notificationService.EnsureDefaultDailyReminderAsync();
        }
        catch
        {
            // Bildirim izni cihaz/OS durumuna göre reddedilebilir; uygulama akışını bozmayalım.
        }
    }
}

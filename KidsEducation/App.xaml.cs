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

        // İlk açılış değilse onboarding'i atla
        if (Preferences.Default.Get("onboarding_done", false))
            shell.GoToAsync("//profileselection");

        return new Window(shell);
    }
}

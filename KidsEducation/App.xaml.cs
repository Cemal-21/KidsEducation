namespace KidsEducation;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        new Services.AppPreferencesService().ApplyTheme();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}

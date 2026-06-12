namespace KidsEducation.Views.Controls;

public partial class BottomTabBar : ContentView
{
    public static readonly BindableProperty IsHomeActiveProperty =
        BindableProperty.Create(nameof(IsHomeActive), typeof(bool), typeof(BottomTabBar), false,
            propertyChanged: (b, o, n) => ((BottomTabBar)b).UpdateTab());

    public static readonly BindableProperty IsProgressActiveProperty =
        BindableProperty.Create(nameof(IsProgressActive), typeof(bool), typeof(BottomTabBar), false,
            propertyChanged: (b, o, n) => ((BottomTabBar)b).UpdateTab());

    public static readonly BindableProperty IsAchievementsActiveProperty =
        BindableProperty.Create(nameof(IsAchievementsActive), typeof(bool), typeof(BottomTabBar), false,
            propertyChanged: (b, o, n) => ((BottomTabBar)b).UpdateTab());

    public static readonly BindableProperty IsProfileActiveProperty =
        BindableProperty.Create(nameof(IsProfileActive), typeof(bool), typeof(BottomTabBar), false,
            propertyChanged: (b, o, n) => ((BottomTabBar)b).UpdateTab());

    public bool IsHomeActive
    {
        get => (bool)GetValue(IsHomeActiveProperty);
        set => SetValue(IsHomeActiveProperty, value);
    }

    public bool IsProgressActive
    {
        get => (bool)GetValue(IsProgressActiveProperty);
        set => SetValue(IsProgressActiveProperty, value);
    }

    public bool IsAchievementsActive
    {
        get => (bool)GetValue(IsAchievementsActiveProperty);
        set => SetValue(IsAchievementsActiveProperty, value);
    }

    public bool IsProfileActive
    {
        get => (bool)GetValue(IsProfileActiveProperty);
        set => SetValue(IsProfileActiveProperty, value);
    }

    public BottomTabBar()
    {
        InitializeComponent();
    }

    private void UpdateTab()
    {
        SetActive(HomeIcon, HomeLabel, HomeDot, IsHomeActive);
        SetActive(ProgressIcon, ProgressLabel, ProgressDot, IsProgressActive);
        SetActive(AchievementsIcon, AchievementsLabel, AchievementsDot, IsAchievementsActive);
        SetActive(ProfileIcon, ProfileLabel, ProfileDot, IsProfileActive);
    }

    private static void SetActive(Image icon, Label label, BoxView dot, bool active)
    {
        icon.Opacity = active ? 1.0 : 0.4;
        icon.Scale = active ? 1.08 : 1.0;
        label.Opacity = active ? 1.0 : 0.62;
        label.TextColor = active
            ? Color.FromArgb("#5148D4")
            : Color.FromArgb("#667085");
        dot.Opacity = active ? 1.0 : 0.0;
    }

    private async void OnHomeTapped(object sender, TappedEventArgs e)
    {
        if (!IsHomeActive)
            await Shell.Current.GoToAsync("//home");
    }

    private async void OnProgressTapped(object sender, TappedEventArgs e)
    {
        if (!IsProgressActive)
            await Shell.Current.GoToAsync("//progress");
    }

    private async void OnAchievementsTapped(object sender, TappedEventArgs e)
    {
        if (!IsAchievementsActive)
            await Shell.Current.GoToAsync("//achievements");
    }

    private async void OnProfileTapped(object sender, TappedEventArgs e)
    {
        if (!IsProfileActive)
            await Shell.Current.GoToAsync("//profile");
    }
}

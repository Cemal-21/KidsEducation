namespace KidsEducation.Views;

/// <summary>
/// Tüm sayfalar bu sınıftan türetilirse otomatik giriş animasyonu alır.
/// ContentPage yerine AnimatedPage kullan.
/// </summary>
public class AnimatedPage : ContentPage
{
    private bool _hasPlayedEnterAnimation;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (Content is null) return;

        if (_hasPlayedEnterAnimation)
        {
            Content.Opacity = 1;
            Content.Scale = 1;
            Content.TranslationX = 0;
            Content.TranslationY = 0;
            return;
        }

        _hasPlayedEnterAnimation = true;

        // Yatay kaydırma tab geçişlerinde zıplama hissi yaratabiliyor;
        // küçük bir fade/pop aynı canlılığı daha stabil verir.
        Content.Opacity = 0;
        Content.Scale = 0.985;
        Content.TranslationY = 8;

        await Task.WhenAll(
            Content.FadeToAsync(1, 220, Easing.CubicOut),
            Content.ScaleToAsync(1, 240, Easing.CubicOut),
            Content.TranslateToAsync(0, 0, 240, Easing.CubicOut)
        );
    }
}

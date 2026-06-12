namespace KidsEducation.Services;

public class AnimationService
{
    // ── Dokunma geri bildirimi ────────────────────────────────

    /// <summary>
    /// Scale + renk flash + hafif titreme.
    /// Herhangi bir View'a uygulanır.
    /// </summary>
    public async Task BounceAsync(View view, bool isCorrect = true)
    {
        // Scale küçül → büyü → normal
        await view.ScaleTo(0.92, 80, Easing.SinIn);
        await view.ScaleTo(1.06, 100, Easing.SpringOut);
        await view.ScaleTo(1.00, 80, Easing.SinOut);
    }

    /// <summary>
    /// Yanlış cevap için titreme animasyonu
    /// </summary>
    public async Task ShakeAsync(View view)
    {
        var original = view.TranslationX;
        await view.TranslateTo(original - 10, 0, 50);
        await view.TranslateTo(original + 10, 0, 50);
        await view.TranslateTo(original - 8, 0, 40);
        await view.TranslateTo(original + 8, 0, 40);
        await view.TranslateTo(original - 4, 0, 30);
        await view.TranslateTo(original, 0, 30);
    }

    /// <summary>
    /// Doğru cevap: scale + yeşil flash
    /// </summary>
    public async Task CorrectAnswerAsync(View view)
    {
        if (view is Border border)
        {
            var original = border.BackgroundColor;
            border.BackgroundColor = Color.FromArgb("#4CAF50");
            await BounceAsync(view);
            await Task.Delay(120);
            border.BackgroundColor = original;
        }
        else
        {
            await BounceAsync(view);
        }
    }

    /// <summary>
    /// Yanlış cevap: shake + kırmızı flash
    /// </summary>
    public async Task WrongAnswerAsync(View view)
    {
        if (view is Border border)
        {
            var original = border.BackgroundColor;
            border.BackgroundColor = Color.FromArgb("#FF3B30");
            await ShakeAsync(view);
            await Task.Delay(120);
            border.BackgroundColor = original;
        }
        else
        {
            await ShakeAsync(view);
        }
    }

    /// <summary>
    /// Buton dokunma geri bildirimi — scale + hafif opacity
    /// </summary>
    public async Task TapFeedbackAsync(View view)
    {
        view.Opacity = 0.7;
        await view.ScaleTo(0.94, 80, Easing.SinIn);
        await view.ScaleTo(1.00, 120, Easing.SpringOut);
        view.Opacity = 1.0;
    }

    /// <summary>
    /// Kart seçme animasyonu (kategori kartı vb.)
    /// </summary>
    public async Task CardSelectAsync(View view)
    {
        await view.ScaleTo(0.95, 80, Easing.SinIn);
        await view.ScaleTo(1.03, 120, Easing.SpringOut);
        await view.ScaleTo(1.00, 80, Easing.SinOut);
    }

    /// <summary>
    /// Yıldız kazanma animasyonu
    /// </summary>
    public async Task StarPopAsync(View view)
    {
        await view.ScaleTo(0, 0);
        view.IsVisible = true;
        await view.ScaleTo(1.4, 200, Easing.SpringOut);
        await view.ScaleTo(1.0, 120, Easing.SinIn);
    }

    // ── Sayfa geçiş animasyonları ─────────────────────────────

    /// <summary>
    /// Sayfa giriş animasyonu — sağdan slide + fade in
    /// ContentPage.OnAppearing() içinde çağır
    /// </summary>
    public async Task PageEnterAsync(View view)
    {
        view.TranslationX = 60;
        view.Opacity = 0;

        await Task.WhenAll(
            view.TranslateTo(0, 0, 350, Easing.CubicOut),
            view.FadeTo(1, 300, Easing.CubicOut)
        );
    }

    /// <summary>
    /// Sayfa çıkış animasyonu — sola slide + fade out
    /// Sayfa kapanmadan önce çağır
    /// </summary>
    public async Task PageExitAsync(View view)
    {
        await Task.WhenAll(
            view.TranslateTo(-40, 0, 250, Easing.CubicIn),
            view.FadeTo(0, 200, Easing.CubicIn)
        );
    }

    /// <summary>
    /// Alt tab geçişi — yukarıdan aşağı slide + fade
    /// </summary>
    public async Task TabEnterAsync(View view)
    {
        view.TranslationY = -20;
        view.Opacity = 0;

        await Task.WhenAll(
            view.TranslateTo(0, 0, 300, Easing.CubicOut),
            view.FadeTo(1, 250, Easing.CubicOut)
        );
    }

    /// <summary>
    /// Liste öğesi giriş animasyonu — staggered
    /// </summary>
    public async Task StaggerEnterAsync(IList<View> views, int delayMs = 60)
    {
        // Önce hepsini gizle
        foreach (var v in views)
        {
            v.Opacity = 0;
            v.TranslationY = 20;
        }

        // Sırayla göster
        foreach (var v in views)
        {
            _ = Task.WhenAll(
                v.FadeTo(1, 200, Easing.CubicOut),
                v.TranslateTo(0, 0, 200, Easing.CubicOut)
            );
            await Task.Delay(delayMs);
        }
    }
}

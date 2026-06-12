namespace KidsEducation.Views.Controls;

/// <summary>
/// Dokunulduğunda otomatik scale + opacity animasyonu yapan Border.
/// Mevcut Border'ların yerine kullanılır, Command bağlanabilir.
/// </summary>
public partial class TappableBorder : Border
{
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(System.Windows.Input.ICommand),
            typeof(TappableBorder));

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object),
            typeof(TappableBorder));

    public System.Windows.Input.ICommand? Command
    {
        get => (System.Windows.Input.ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public TappableBorder()
    {
        InitializeComponent();

        var tap = new TapGestureRecognizer();
        tap.Tapped += OnTapped;
        GestureRecognizers.Add(tap);
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($">>>>>>>>>> TAPPED. BindingContext type: {this.BindingContext?.GetType().Name ?? "NULL"}, Command null mu: {Command is null}, CanExecute: {Command?.CanExecute(CommandParameter)}");

        if (Command?.CanExecute(CommandParameter) == true)
            Command.Execute(CommandParameter);

        _ = AnimateAsync();
    }

    private async Task AnimateAsync()
    {
        try
        {
            // Önceki animasyon kalıntılarını temizle, scale/opacity'yi sıfırla
            this.AbortAnimation("TappableBorderAnimation");
            this.Scale = 1.0;
            this.Opacity = 1.0;

            await this.ScaleTo(0.94, 80, Easing.SinIn);
            await this.ScaleTo(1.03, 120, Easing.SpringOut);
            await this.ScaleTo(1.00, 80, Easing.SinOut);

            this.Opacity = 0.75;
            await Task.Delay(60);
            this.Opacity = 1.0;
        }
        catch
        {
            // Animasyon hatası UI/command akışını asla bozmamalı
            this.Scale = 1.0;
            this.Opacity = 1.0;
        }
    }
}
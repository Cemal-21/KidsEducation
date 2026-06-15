using KidsEducation.ViewModels.Game;
using Microsoft.Maui.Graphics;

namespace KidsEducation.Views.Game;

public partial class DrawingGamePage : ContentPage
{
    private readonly DrawingGameViewModel _vm;
    private readonly DrawingGameDrawable _drawable;

    public DrawingGamePage(DrawingGameViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        _drawable = new DrawingGameDrawable { ViewModel = vm };
        DrawCanvas.Drawable = _drawable;

        vm.RequestRedraw = () => MainThread.BeginInvokeOnMainThread(() =>
        {
            DrawCanvas.Invalidate();
            UpdateResultSheet();
        });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.InitializeCommand.Execute(null);
    }

    private void UpdateResultSheet()
    {
        bool showResult = _vm.State == DrawingGameState.Result;
        ResultSheet.IsVisible = showResult;
        DrawingBar.IsVisible = !showResult;

        if (showResult)
        {
            ResultEmoji.Text = _vm.IsCorrect ? "🎉" : "😅";
        }
    }

    private SizeF CanvasSize => new((float)DrawCanvas.Width, (float)DrawCanvas.Height);

    private void DrawCanvas_StartInteraction(object sender, TouchEventArgs e)
    {
        var touch = ToPointF(e.Touches.FirstOrDefault());
        _vm.OnStrokeStart(touch);
    }

    private void DrawCanvas_DragInteraction(object sender, TouchEventArgs e)
    {
        var touch = ToPointF(e.Touches.FirstOrDefault());
        _vm.OnStrokeMove(touch);
    }

    private void DrawCanvas_EndInteraction(object sender, TouchEventArgs e) =>
        _vm.OnStrokeEnd();

    private void DrawCanvas_CancelInteraction(object sender, EventArgs e) =>
        _vm.OnStrokeEnd();

    // RecognizeCommand'a canvas boyutunu CommandParameter olarak gönderemediğimiz için
    // "Tahmin Et" butonuna ek tap handler ekliyoruz
    private void OnRecognizeTapped(object sender, TappedEventArgs e) =>
        _vm.RecognizeCommand.Execute(CanvasSize);

    private static PointF ToPointF(Point p) => new((float)p.X, (float)p.Y);
}

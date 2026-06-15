using KidsEducation.ViewModels.Game;
using Microsoft.Maui.Graphics;

namespace KidsEducation.Views.Game;

public partial class ConnectDotsGamePage : ContentPage
{
    private readonly ConnectDotsGameViewModel _vm;
    private readonly ConnectDotsDrawable _drawable;

    public ConnectDotsGamePage(ConnectDotsGameViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        _drawable = new ConnectDotsDrawable { ViewModel = vm };
        DotsCanvas.Drawable = _drawable;

        vm.RequestRedraw = () => MainThread.BeginInvokeOnMainThread(() =>
        {
            DotsCanvas.Invalidate();
            UpdateProgress();
        });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.InitializeCommand.Execute(null);
    }

    private void UpdateProgress()
    {
        if (_vm.CurrentShape is null) return;
        double total = _vm.CurrentShape.Dots.Count;
        ProgressIndicator.Progress = total > 0 ? _vm.NextDotIndex / total : 0;
    }

    private SizeF CanvasSize => new((float)DotsCanvas.Width, (float)DotsCanvas.Height);

    private void DotsCanvas_StartInteraction(object sender, TouchEventArgs e)
    {
        var touch = ToPointF(e.Touches.FirstOrDefault());
        _vm.UpdateTouchPoint(touch);
        _vm.TryConnectDot(touch, CanvasSize);
    }

    private void DotsCanvas_DragInteraction(object sender, TouchEventArgs e)
    {
        var touch = ToPointF(e.Touches.FirstOrDefault());
        _vm.UpdateTouchPoint(touch);
        _vm.TryConnectDot(touch, CanvasSize);
    }

    private void DotsCanvas_EndInteraction(object sender, TouchEventArgs e) =>
        _vm.ClearTouch();

    private void DotsCanvas_CancelInteraction(object sender, EventArgs e) =>
        _vm.ClearTouch();

    private static PointF ToPointF(Point p) => new((float)p.X, (float)p.Y);
}

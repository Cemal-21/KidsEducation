using KidsEducation.ViewModels.Game;
using Microsoft.Maui.Graphics;

namespace KidsEducation.Views.Game;

public partial class ShapeColoringGamePage : ContentPage
{
    private readonly ShapeColoringGameViewModel _viewModel;
    private readonly ShapeColoringDrawable _referenceDrawable;
    private readonly ShapeColoringDrawable _paintDrawable;

    public ShapeColoringGamePage(ShapeColoringGameViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        _referenceDrawable = new ShapeColoringDrawable { ViewModel = viewModel, IsReference = true };
        _paintDrawable = new ShapeColoringDrawable { ViewModel = viewModel, IsReference = false };
        ReferenceCanvas.Drawable = _referenceDrawable;
        PaintCanvas.Drawable = _paintDrawable;

        viewModel.RequestRedraw = () => MainThread.BeginInvokeOnMainThread(() =>
        {
            ReferenceCanvas.Invalidate();
            PaintCanvas.Invalidate();
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeCommand.ExecuteAsync(null);
        ReferenceCanvas.Invalidate();
        PaintCanvas.Invalidate();
    }

    private SizeF PaintCanvasSize => new((float)PaintCanvas.Width, (float)PaintCanvas.Height);

    private async void PaintCanvas_StartInteraction(object sender, TouchEventArgs e) =>
        await PaintAsync(e);

    private async void PaintCanvas_DragInteraction(object sender, TouchEventArgs e) =>
        await PaintAsync(e);

    private async Task PaintAsync(TouchEventArgs e)
    {
        var touch = e.Touches.FirstOrDefault();
        await _viewModel.PaintAtAsync(new PointF((float)touch.X, (float)touch.Y), PaintCanvasSize);
    }
}

using KidsEducation.ViewModels.Game;

namespace KidsEducation.Views.Game;

[QueryProperty(nameof(CategoryId), "categoryId")]
public partial class TracingGamePage : ContentPage
{
    private readonly TracingGameViewModel _viewModel;

    public string? CategoryId { get; set; }

    public TracingGamePage(TracingGameViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeCommand.ExecuteAsync(CategoryId);
    }

    private void OnStartInteraction(object sender, TouchEventArgs e)
    {
        if (e.Touches.Length == 0) return;
        _viewModel.TracingDrawable.StartStroke(e.Touches[0]);
        TracingCanvas.Invalidate();
    }

    private void OnDragInteraction(object sender, TouchEventArgs e)
    {
        if (e.Touches.Length == 0) return;
        _viewModel.TracingDrawable.AddPoint(e.Touches[0]);
        _viewModel.UpdateStrokeLength(TracingCanvas.Width, TracingCanvas.Height);
        TracingCanvas.Invalidate();
    }

    private void OnEndInteraction(object sender, TouchEventArgs e)
    {
        _viewModel.TracingDrawable.EndStroke();
    }
}

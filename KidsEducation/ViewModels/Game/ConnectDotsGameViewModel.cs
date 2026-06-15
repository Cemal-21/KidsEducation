using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;
using Microsoft.Maui.Graphics;

namespace KidsEducation.ViewModels.Game;

public partial class ConnectDotsGameViewModel : ObservableObject
{
    private readonly DotShapeService _shapeService;
    private readonly AudioService _audioService;
    private readonly NavigationService _navService;

    [ObservableProperty] private DotShape? _currentShape;
    [ObservableProperty] private int _nextDotIndex;
    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private string _hintText = "";
    [ObservableProperty] private string _completionText = "";
    [ObservableProperty] private string _shapeCounter = "";

    public PointF CurrentTouchPoint { get; private set; }
    public bool IsTouching { get; private set; }

    public Action? RequestRedraw { get; set; }

    public ConnectDotsGameViewModel(
        DotShapeService shapeService,
        AudioService audioService,
        NavigationService navService)
    {
        _shapeService = shapeService;
        _audioService = audioService;
        _navService = navService;
    }

    [RelayCommand]
    public void Initialize() => LoadNextShape();

    private void LoadNextShape()
    {
        CurrentShape = _shapeService.GetNext();
        NextDotIndex = 0;
        IsCompleted = false;
        CompletionText = "";
        UpdateHint();
        RequestRedraw?.Invoke();
    }

    public bool TryConnectDot(PointF touch, SizeF canvasSize)
    {
        if (IsCompleted || CurrentShape is null) return false;
        if (NextDotIndex >= CurrentShape.Dots.Count) return false;

        var nextDot = CurrentShape.Dots[NextDotIndex];
        var dotPos = ToScreen(nextDot, canvasSize);
        var dist = Distance(touch, dotPos);

        if (dist > 48f) return false;

        NextDotIndex++;
        HapticService.Light();

        if (NextDotIndex >= CurrentShape.Dots.Count)
        {
            IsCompleted = true;
            HapticService.Success();
            IsTouching = false;
            CompletionText = $"{CurrentShape.Emoji} {CurrentShape.NameTr}!";
            _ = _audioService.SpeakTextAsync(CurrentShape.NameTr);
        }
        else
        {
            UpdateHint();
        }

        RequestRedraw?.Invoke();
        return true;
    }

    public void UpdateTouchPoint(PointF touch)
    {
        CurrentTouchPoint = touch;
        IsTouching = true;
        RequestRedraw?.Invoke();
    }

    public void ClearTouch()
    {
        IsTouching = false;
        RequestRedraw?.Invoke();
    }

    private void UpdateHint()
    {
        if (CurrentShape is null) return;
        HintText = NextDotIndex == 0
            ? "1 numaralı noktadan başla! 👆"
            : $"Şimdi {NextDotIndex + 1} numaralı noktayı bul!";
    }

    [RelayCommand]
    public void NextShape() => LoadNextShape();

    [RelayCommand]
    public Task GoBackAsync() => _navService.GoBackAsync();

    public static PointF ToScreen(DotPoint dot, SizeF size) =>
        new(dot.NX * size.Width, dot.NY * size.Height);

    private static float Distance(PointF a, PointF b) =>
        MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}

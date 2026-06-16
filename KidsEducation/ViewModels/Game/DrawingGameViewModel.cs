using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;
using Microsoft.Maui.Graphics;

namespace KidsEducation.ViewModels.Game;

public partial class DrawingGameViewModel : ObservableObject
{
    private readonly DrawingRecognitionService _recognizer;
    private readonly AudioService _audioService;
    private readonly NavigationService _navService;

    private readonly List<DrawingChallenge> _challenges;
    private int _challengeIndex;

    [ObservableProperty] private DrawingChallenge? _currentChallenge;
    [ObservableProperty] private DrawingGameState _state = DrawingGameState.Drawing;
    [ObservableProperty] private string _resultText = "";
    [ObservableProperty] private string _resultSubText = "";
    [ObservableProperty] private bool _isCorrect;
    [ObservableProperty] private string _scoreText = "";
    [ObservableProperty] private int _score;
    [ObservableProperty] private int _round = 1;
    [ObservableProperty] private int _totalRounds = 10;

    // Çizim verisi (drawable ile paylaşılır)
    public List<List<PointF>> Strokes { get; } = new();
    public List<PointF>? CurrentStroke { get; private set; }

    public Action? RequestRedraw { get; set; }

    public DrawingGameViewModel(
        DrawingRecognitionService recognizer,
        AudioService audioService,
        NavigationService navService)
    {
        _recognizer = recognizer;
        _audioService = audioService;
        _navService = navService;

        _challenges = DrawingRecognitionService.Challenges
            .OrderBy(_ => Random.Shared.Next())
            .ToList();
    }

    [RelayCommand]
    public void Initialize()
    {
        _challengeIndex = 0;
        Score = 0;
        Round = 1;
        LoadChallenge();
    }

    private void LoadChallenge()
    {
        if (_challengeIndex >= _challenges.Count)
            _challengeIndex = 0;

        CurrentChallenge = _challenges[_challengeIndex];
        State = DrawingGameState.Drawing;
        ResultText = "";
        ResultSubText = "";
        Strokes.Clear();
        CurrentStroke = null;
        UpdateScoreText();
        RequestRedraw?.Invoke();
    }

    // ── Çizim olayları ───────────────────────────────────────────────────────

    public void OnStrokeStart(PointF point)
    {
        if (State != DrawingGameState.Drawing) return;
        CurrentStroke = new List<PointF> { point };
        RequestRedraw?.Invoke();
    }

    public void OnStrokeMove(PointF point)
    {
        if (State != DrawingGameState.Drawing || CurrentStroke is null) return;
        CurrentStroke.Add(point);
        RequestRedraw?.Invoke();
    }

    public void OnStrokeEnd()
    {
        if (CurrentStroke is { Count: > 2 })
        {
            Strokes.Add(new List<PointF>(CurrentStroke));
        }
        CurrentStroke = null;
        RequestRedraw?.Invoke();
    }

    // ── Tahmin et ────────────────────────────────────────────────────────────

    [RelayCommand]
    public void Recognize(SizeF canvasSize)
    {
        if (State != DrawingGameState.Drawing) return;

        var allPoints = Strokes.SelectMany(s => s).ToList();
        if (CurrentStroke is { Count: > 2 })
        {
            allPoints.AddRange(CurrentStroke);
        }
        if (allPoints.Count < 15)
        {
            ResultText = "Daha fazla çiz! ✏️";
            ResultSubText = "Şekli daha belirgin çizmeye çalış.";
            State = DrawingGameState.TooShort;
            RequestRedraw?.Invoke();
            return;
        }

        var strokesForRecognition = Strokes.Select(s => s.ToList()).ToList();
        if (CurrentStroke is { Count: > 2 })
        {
            strokesForRecognition.Add(new List<PointF>(CurrentStroke));
        }

        var result = _recognizer.Recognize(strokesForRecognition, canvasSize);
        var targetResult = CurrentChallenge is null
            ? result
            : _recognizer.RecognizeTarget(strokesForRecognition, canvasSize, CurrentChallenge.ShapeType);

        if (result.ErrorMessage is not null)
        {
            ResultText = result.ErrorMessage;
            State = DrawingGameState.TooShort;
            return;
        }

        var recognized = DrawingRecognitionService.Challenges
            .FirstOrDefault(c => c.ShapeType == result.ShapeType);

        bool directMatch = recognized?.Id == CurrentChallenge?.Id;
        bool closeEnoughForKids = targetResult.Confidence >= 38f
            || (targetResult.Confidence >= 28f && result.Confidence - targetResult.Confidence <= 20f);
        bool correct = directMatch || closeEnoughForKids;
        IsCorrect = correct;

        if (correct)
        {
            HapticService.Success();
            var confidence = Math.Max(result.Confidence, targetResult.Confidence);
            Score += confidence > 70f ? 10 : 7;
            ResultText = $"Doğru! {CurrentChallenge!.Emoji}";
            ResultSubText = $"Sen bir {CurrentChallenge.NameTr} çizdin! 🎉";
            _ = _audioService.SpeakTextAsync(CurrentChallenge.NameTr);
        }
        else
        {
            HapticService.Error();
            ResultText = $"Bu bir {recognized?.NameTr ?? "?"} gibi görünüyor {recognized?.Emoji}";
            ResultSubText = $"Ama sen {CurrentChallenge!.NameTr} çizmeye çalışıyordun. Tekrar dene!";
        }

        UpdateScoreText();
        State = DrawingGameState.Result;
        RequestRedraw?.Invoke();
    }

    [RelayCommand]
    public void TryAgain()
    {
        State = DrawingGameState.Drawing;
        Strokes.Clear();
        CurrentStroke = null;
        ResultText = "";
        RequestRedraw?.Invoke();
    }

    [RelayCommand]
    public void NextChallenge()
    {
        _challengeIndex++;
        Round = Math.Min(Round + 1, TotalRounds);
        LoadChallenge();
    }

    [RelayCommand]
    public void ClearCanvas()
    {
        Strokes.Clear();
        CurrentStroke = null;
        RequestRedraw?.Invoke();
    }

    [RelayCommand]
    public Task GoBackAsync() => _navService.GoBackAsync();

    private void UpdateScoreText() =>
        ScoreText = $"{Round}/{TotalRounds}  ⭐ {Score}";
}

public enum DrawingGameState
{
    Drawing,
    TooShort,
    Result,
}

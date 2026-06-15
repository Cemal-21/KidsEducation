using CommunityToolkit.Maui.Media;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace KidsEducation.ViewModels.Pronunciation;

public enum PronunciationState { Idle, Listening, Correct, Wrong, NoPermission }

public partial class PronunciationWordCard : ObservableObject
{
    public string ItemId { get; set; } = string.Empty;
    public string NameTr { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string Emoji { get; set; } = "🔊";

    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private bool _isCorrect;

    public string StatusEmoji => IsCompleted ? (IsCorrect ? "✅" : "❌") : "🎤";
    partial void OnIsCompletedChanged(bool v) => OnPropertyChanged(nameof(StatusEmoji));
    partial void OnIsCorrectChanged(bool v)   => OnPropertyChanged(nameof(StatusEmoji));
}

public partial class PronunciationGameViewModel : ObservableObject
{
    private readonly ISpeechToText _speechToText;
    private readonly AudioService _audioService;
    private readonly ContentService _contentService;
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;

    private const int WordsPerRound = 6;
    private List<LearningItem> _allItems = new();
    private CancellationTokenSource? _listenCts;

    [ObservableProperty] private string _categoryId = "animals";
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private PronunciationState _state = PronunciationState.Idle;
    [ObservableProperty] private PronunciationWordCard? _currentWord;
    [ObservableProperty] private string _feedbackText = "";
    [ObservableProperty] private string _feedbackEmoji = "";
    [ObservableProperty] private string _heardText = "";
    [ObservableProperty] private int _correctCount;
    [ObservableProperty] private int _totalAnswered;
    [ObservableProperty] private bool _isFinished;
    [ObservableProperty] private int _currentIndex;
    [ObservableProperty] private int _totalWords;

    // Durum hesaplamaları
    public bool IsIdle      => State == PronunciationState.Idle;
    public bool IsListening => State == PronunciationState.Listening;
    public bool IsCorrect   => State == PronunciationState.Correct;
    public bool IsWrong     => State == PronunciationState.Wrong;
    public bool IsNoPermission => State == PronunciationState.NoPermission;
    public bool ShowFeedback => State is PronunciationState.Correct or PronunciationState.Wrong;
    public double Progress  => TotalWords == 0 ? 0 : (double)TotalAnswered / TotalWords;
    public string ScoreText => $"{CorrectCount}/{TotalAnswered}";

    public string MicButtonText => State switch
    {
        PronunciationState.Listening => "🛑  Dinleniyor...",
        PronunciationState.Correct   => "✅  Doğru!",
        PronunciationState.Wrong     => "❌  Tekrar Dene",
        _                            => "🎤  Söyle!"
    };

    public string MicButtonColor => State switch
    {
        PronunciationState.Listening => "#EF4444",
        PronunciationState.Correct   => "#16A34A",
        PronunciationState.Wrong     => "#EA580C",
        _                            => "#5148D4"
    };

    public ObservableCollection<PronunciationWordCard> Words { get; } = new();

    partial void OnStateChanged(PronunciationState v)
    {
        OnPropertyChanged(nameof(IsIdle));
        OnPropertyChanged(nameof(IsListening));
        OnPropertyChanged(nameof(IsCorrect));
        OnPropertyChanged(nameof(IsWrong));
        OnPropertyChanged(nameof(IsNoPermission));
        OnPropertyChanged(nameof(ShowFeedback));
        OnPropertyChanged(nameof(MicButtonText));
        OnPropertyChanged(nameof(MicButtonColor));
    }

    partial void OnTotalAnsweredChanged(int v)
    {
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(ScoreText));
    }

    public PronunciationGameViewModel(
        ISpeechToText speechToText,
        AudioService audioService,
        ContentService contentService,
        ProfileService profileService,
        NavigationService navigationService)
    {
        _speechToText = speechToText;
        _audioService = audioService;
        _contentService = contentService;
        _profileService = profileService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public async Task InitializeAsync(string categoryId = "animals")
    {
        if (!string.IsNullOrWhiteSpace(categoryId))
            CategoryId = categoryId;

        IsLoading = true;
        IsFinished = false;
        CorrectCount = 0;
        TotalAnswered = 0;
        Words.Clear();

        try
        {
            var profile = _profileService.GetActiveProfile();
            _allItems = await _contentService.GetItemsAsync(CategoryId);

            var selected = _allItems
                .OrderBy(_ => Random.Shared.Next())
                .Take(WordsPerRound)
                .ToList();

            TotalWords = selected.Count;

            foreach (var item in selected)
            {
                Words.Add(new PronunciationWordCard
                {
                    ItemId = item.Id,
                    NameTr = item.NameTr,
                    ImagePath = item.ImagePath,
                });
            }

            CurrentIndex = 0;
            CurrentWord = Words.FirstOrDefault();
            State = PronunciationState.Idle;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SpeakWordAsync()
    {
        if (CurrentWord is null) return;
        await _audioService.PlayItemSoundAsync(CurrentWord.ItemId);
    }

    [RelayCommand]
    public async Task ListenAsync()
    {
        if (CurrentWord is null) return;

        if (State == PronunciationState.Listening)
        {
            await StopListeningAsync();
            return;
        }

        // Mikrofon izni kontrolü
        var permGranted = await _speechToText.RequestPermissions(CancellationToken.None);
        if (!permGranted)
        {
            State = PronunciationState.NoPermission;
            FeedbackEmoji = "🎙️";
            FeedbackText = "Mikrofon izni gerekli. Lütfen ayarlardan izin ver.";
            return;
        }

        State = PronunciationState.Listening;
        HeardText = "";
        FeedbackText = "Dinliyorum... kelimeyi söyle!";
        FeedbackEmoji = "👂";

        _listenCts = new CancellationTokenSource(TimeSpan.FromSeconds(7));

        _speechToText.RecognitionResultUpdated += OnPartialResult;
        _speechToText.RecognitionResultCompleted += OnFinalResult;

        try
        {
            await _speechToText.StartListenAsync(
                new SpeechToTextOptions { Culture = new CultureInfo("tr-TR"), ShouldReportPartialResults = true },
                _listenCts.Token);

            // Maksimum 7 saniye bekle — token iptalinde otomatik durur
            await Task.Delay(7000, _listenCts.Token).ConfigureAwait(false);
            await StopListeningAsync();
        }
        catch (OperationCanceledException) { /* normal akış */ }
        catch
        {
            await StopListeningAsync();
            if (State == PronunciationState.Listening)
            {
                State = PronunciationState.Idle;
                FeedbackText = "Bir hata oluştu, tekrar dene.";
            }
        }
        finally
        {
            _speechToText.RecognitionResultUpdated -= OnPartialResult;
            _speechToText.RecognitionResultCompleted -= OnFinalResult;
        }
    }

    private async Task StopListeningAsync()
    {
        _listenCts?.Cancel();
        try { await _speechToText.StopListenAsync(CancellationToken.None); } catch { }
    }

    private void OnPartialResult(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() => HeardText = e.RecognitionResult);
    }

    private void OnFinalResult(object? sender, SpeechToTextRecognitionResultCompletedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _speechToText.RecognitionResultUpdated -= OnPartialResult;
            _speechToText.RecognitionResultCompleted -= OnFinalResult;

            if (e.RecognitionResult.IsSuccessful)
            {
                var heard = e.RecognitionResult.Text?.Trim().ToLowerInvariant() ?? "";
                HeardText = e.RecognitionResult.Text ?? "";
                await EvaluateAsync(heard);
            }
            else if (State == PronunciationState.Listening)
            {
                State = PronunciationState.Idle;
                HeardText = "";
                FeedbackText = "Seni duyamadım, tekrar dene!";
                FeedbackEmoji = "🤔";
            }
        });
    }

    private async Task EvaluateAsync(string heard)
    {
        if (CurrentWord is null) return;

        var target = NormalizeTr(CurrentWord.NameTr);
        var isCorrect = heard.Contains(target) || target.Contains(heard) ||
                        LevenshteinMatch(heard, target, maxDist: 2);

        CurrentWord.IsCompleted = true;
        CurrentWord.IsCorrect = isCorrect;
        TotalAnswered++;

        if (isCorrect)
        {
            CorrectCount++;
            State = PronunciationState.Correct;
            FeedbackEmoji = "🎉";
            FeedbackText = "Harika! Çok güzel söyledin!";
            await _audioService.PlayCorrectAsync();
        }
        else
        {
            State = PronunciationState.Wrong;
            FeedbackEmoji = "💪";
            FeedbackText = $"Duyduğum: \"{HeardText}\"\nDoğrusu: \"{CurrentWord.NameTr}\"";
            await _audioService.PlayWrongAsync();
        }

        await Task.Delay(1800);
        await AdvanceAsync();
    }

    private async Task AdvanceAsync()
    {
        CurrentIndex++;
        if (CurrentIndex >= Words.Count)
        {
            IsFinished = true;
            State = PronunciationState.Idle;
            FeedbackEmoji = CorrectCount >= Words.Count * 0.8 ? "🏆" : "👏";
            FeedbackText = $"{CorrectCount}/{Words.Count} kelimeyi doğru söyledin!";
            return;
        }

        CurrentWord = Words[CurrentIndex];
        State = PronunciationState.Idle;
        FeedbackText = "";
        HeardText = "";

        // Yeni kelimeyi otomatik seslendirme
        await Task.Delay(400);
        await _audioService.PlayItemSoundAsync(CurrentWord.ItemId);
    }

    [RelayCommand]
    public async Task RestartAsync() => await InitializeAsync(CategoryId);

    [RelayCommand]
    public async Task GoBackAsync()
    {
        _listenCts?.Cancel();
        await _navigationService.GoBackAsync();
    }

    // ── Yardımcı metodlar ────────────────────────────────────

    private static string NormalizeTr(string s) =>
        s.ToLowerInvariant()
         .Replace("ı", "i").Replace("ğ", "g").Replace("ş", "s")
         .Replace("ç", "c").Replace("ö", "o").Replace("ü", "u")
         .Trim();

    private static bool LevenshteinMatch(string a, string b, int maxDist)
    {
        if (Math.Abs(a.Length - b.Length) > maxDist) return false;
        int[,] d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
                d[i, j] = a[i - 1] == b[j - 1]
                    ? d[i - 1, j - 1]
                    : 1 + Math.Min(d[i - 1, j - 1], Math.Min(d[i - 1, j], d[i, j - 1]));
        return d[a.Length, b.Length] <= maxDist;
    }
}

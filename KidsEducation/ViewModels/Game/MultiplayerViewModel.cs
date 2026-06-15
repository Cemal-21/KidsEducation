using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Game;

public partial class MultiplayerViewModel : ObservableObject
{
    private readonly MultiplayerService _mp;
    private readonly ContentService _contentService;
    private readonly ProfileService _profileService;
    private readonly AudioService _audioService;
    private readonly NavigationService _navService;

    private List<LearningItem> _allItems = new();
    private MultiplayerQuestion? _currentQuestion;
    private DateTime _questionSentAt;

    // ── Ortak state ───────────────────────────────────────────────────────────
    [ObservableProperty] private MultiplayerPageState _pageState = MultiplayerPageState.RoleSelect;
    [ObservableProperty] private string _hostIpText = "";
    [ObservableProperty] private string _connectIpInput = "";
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private bool _isBusy;

    // ── Ebeveyn (host) state ─────────────────────────────────────────────────
    [ObservableProperty] private string _hostIp = "";
    [ObservableProperty] private bool _clientConnected;
    [ObservableProperty] private int _round = 0;
    [ObservableProperty] private int _totalRounds = 10;
    [ObservableProperty] private int _childScore;
    [ObservableProperty] private int _parentScore;
    [ObservableProperty] private string _lastAnswerText = "";
    [ObservableProperty] private bool _waitingForAnswer;
    [ObservableProperty] private LearningItem? _currentItem;

    // ── Çocuk (client) state ─────────────────────────────────────────────────
    [ObservableProperty] private MultiplayerQuestion? _activeQuestion;
    [ObservableProperty] private string? _selectedAnswer;
    [ObservableProperty] private bool _answerSubmitted;
    [ObservableProperty] private bool _lastAnswerCorrect;
    [ObservableProperty] private int _childTotalScore;
    [ObservableProperty] private string _option1 = "";
    [ObservableProperty] private string _option2 = "";
    [ObservableProperty] private string _option3 = "";
    [ObservableProperty] private string _option4 = "";

    public MultiplayerViewModel(
        MultiplayerService mp,
        ContentService contentService,
        ProfileService profileService,
        AudioService audioService,
        NavigationService navService)
    {
        _mp = mp;
        _contentService = contentService;
        _profileService = profileService;
        _audioService = audioService;
        _navService = navService;

        _mp.ClientConnected += OnClientConnected;
        _mp.AnswerReceived += OnAnswerReceived;
        _mp.QuestionReceived += OnQuestionReceived;
    }

    // ── Rol seçimi ────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task BeHostAsync()
    {
        IsBusy = true;
        try
        {
            var profile = _profileService.GetActiveProfile();
            if (profile is not null)
                _allItems = await _contentService.GetMixedGameItemsAsync(profile, 60);

            var ip = _mp.StartHost();
            HostIp = ip;
            StatusText = "Çocuğun telefonu bağlanana kadar bekle…";
            PageState = MultiplayerPageState.HostWaiting;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task ConnectAsChildAsync()
    {
        if (string.IsNullOrWhiteSpace(ConnectIpInput)) return;
        IsBusy = true;
        StatusText = "Bağlanıyor…";
        try
        {
            bool ok = await _mp.ConnectToHostAsync(ConnectIpInput.Trim());
            if (ok)
            {
                StatusText = "Bağlandı! Ebeveyn soru gönderene kadar bekle…";
                PageState = MultiplayerPageState.ChildWaiting;
            }
            else
            {
                StatusText = "Bağlanamadı. IP adresini kontrol et.";
            }
        }
        finally { IsBusy = false; }
    }

    // ── Host: soru gönder ────────────────────────────────────────────────────

    [RelayCommand]
    public async Task SendNextQuestionAsync()
    {
        if (_allItems.Count == 0) return;
        Round++;
        WaitingForAnswer = true;
        LastAnswerText = "";

        var item = _allItems[Random.Shared.Next(_allItems.Count)];
        CurrentItem = item;
        _questionSentAt = DateTime.UtcNow;

        // 3 yanlış şık + 1 doğru
        var wrongs = _allItems
            .Where(i => i.Id != item.Id)
            .OrderBy(_ => Random.Shared.Next())
            .Take(3)
            .Select(i => i.NameTr)
            .ToList();

        var options = wrongs.Append(item.NameTr).OrderBy(_ => Random.Shared.Next()).ToList();

        var question = new MultiplayerQuestion
        {
            QuestionText = "Bu nesnenin adı ne?",
            ImagePath = item.ImagePath,
            CorrectAnswer = item.NameTr,
            Options = options,
            CategoryEmoji = "🎯",
            Round = Round,
            TotalRounds = TotalRounds,
        };

        _currentQuestion = question;
        _mp.SendQuestion(question);

        await _audioService.SpeakTextAsync("Soru hazır, bak bakalım!");
    }

    private void OnClientConnected()
    {
        ClientConnected = true;
        StatusText = "Çocuk bağlandı! Soruyu gönder.";
    }

    private void OnAnswerReceived(MultiplayerAnswer answer)
    {
        if (answer.QuestionId != _currentQuestion?.Id) return;

        WaitingForAnswer = false;
        var elapsed = (long)(DateTime.UtcNow - _questionSentAt).TotalMilliseconds;

        if (answer.IsCorrect)
        {
            HapticService.Success();
            ChildScore++;
            LastAnswerText = $"✅ Doğru! Çocuk {ChildScore} puan aldı.";
            _ = _audioService.SpeakTextAsync("Aferin, doğru!");
        }
        else
        {
            LastAnswerText = $"❌ Yanlış. Doğru cevap: {_currentQuestion?.CorrectAnswer}";
            _ = _audioService.SpeakTextAsync("Üzgünüm, yanlış.");
        }

        if (Round >= TotalRounds)
        {
            StatusText = $"Oyun bitti! Çocuk {ChildScore}/{TotalRounds} doğru yaptı.";
            PageState = MultiplayerPageState.GameOver;
        }
    }

    // ── Child: cevap ver ─────────────────────────────────────────────────────

    private void OnQuestionReceived(MultiplayerQuestion question)
    {
        ActiveQuestion = question;
        SelectedAnswer = null;
        AnswerSubmitted = false;
        Option1 = question.Options.Count > 0 ? question.Options[0] : "";
        Option2 = question.Options.Count > 1 ? question.Options[1] : "";
        Option3 = question.Options.Count > 2 ? question.Options[2] : "";
        Option4 = question.Options.Count > 3 ? question.Options[3] : "";
        PageState = MultiplayerPageState.ChildQuestion;
    }

    [RelayCommand]
    public async Task SubmitAnswerAsync(string answer)
    {
        if (AnswerSubmitted || ActiveQuestion is null) return;
        SelectedAnswer = answer;
        AnswerSubmitted = true;

        bool correct = answer == ActiveQuestion.CorrectAnswer;
        LastAnswerCorrect = correct;
        if (correct) { ChildTotalScore++; HapticService.Success(); }
        else HapticService.Error();

        var mp = new MultiplayerAnswer
        {
            QuestionId = ActiveQuestion.Id,
            SelectedAnswer = answer,
            IsCorrect = correct,
        };

        await _mp.SendAnswerAsync(mp);

        if (correct)
            await _audioService.SpeakTextAsync("Doğru!");
        else
            await _audioService.SpeakTextAsync("Yanlış, üzgünüm");

        PageState = MultiplayerPageState.ChildAnswered;
    }

    [RelayCommand]
    public void ResetToRoleSelect()
    {
        _mp.Dispose();
        Round = 0;
        ChildScore = 0;
        ChildTotalScore = 0;
        ActiveQuestion = null;
        PageState = MultiplayerPageState.RoleSelect;
    }

    [RelayCommand]
    public Task GoBackAsync() => _navService.GoBackAsync();
}

public enum MultiplayerPageState
{
    RoleSelect,
    HostWaiting,
    HostQuestion,
    ChildWaiting,
    ChildQuestion,
    ChildAnswered,
    GameOver,
}

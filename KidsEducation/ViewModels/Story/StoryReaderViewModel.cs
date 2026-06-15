using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Services;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace KidsEducation.ViewModels.Story;

// ── Veri modelleri ────────────────────────────────────────────

public class StoryWordData
{
    public string WordTr { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public bool IsKeyword { get; set; }
}

public class StoryData
{
    public string Id { get; set; } = string.Empty;
    public string TitleTr { get; set; } = string.Empty;
    public string Emoji { get; set; } = "📖";
    public string CoverEmoji { get; set; } = "📖";
    public int Difficulty { get; set; } = 1;
    public List<StoryWordData> Words { get; set; } = new();
}

// ── UI modeli ─────────────────────────────────────────────────

public partial class StoryWordItem : ObservableObject
{
    public string WordTr { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public bool IsKeyword { get; set; }

    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private bool _isPast;

    public string TextColor => IsActive ? "#5148D4"
        : IsKeyword && IsPast ? "#21C7A8"
        : IsPast ? "#172033"
        : "#AAAAAA";

    public string FontSize => IsKeyword ? "18" : "16";
    public bool IsBold => IsKeyword;

    partial void OnIsActiveChanged(bool value) => Refresh();
    partial void OnIsPastChanged(bool value)   => Refresh();
    private void Refresh()
    {
        OnPropertyChanged(nameof(TextColor));
    }
}

// ── Ana ViewModel ─────────────────────────────────────────────

public partial class StoryReaderViewModel : ObservableObject
{
    private readonly AudioService _audioService;
    private readonly NavigationService _navigationService;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private string _storyTitle = string.Empty;
    [ObservableProperty] private string _storyEmoji = "📖";
    [ObservableProperty] private int _currentStoryIndex;
    [ObservableProperty] private int _totalStories;
    [ObservableProperty] private bool _isFinished;

    public ObservableCollection<StoryWordItem> Words { get; } = new();
    public ObservableCollection<StoryListItem> StoryList { get; } = new();

    private List<StoryData> _stories = new();
    private CancellationTokenSource? _playCts;

    public StoryReaderViewModel(AudioService audioService, NavigationService navigationService)
    {
        _audioService = audioService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("stories.json");
            using var reader = new System.IO.StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            using var doc = JsonDocument.Parse(json);
            _stories = doc.RootElement
                .GetProperty("stories")
                .Deserialize<List<StoryData>>(JsonOptions) ?? new();

            StoryList.Clear();
            for (int i = 0; i < _stories.Count; i++)
            {
                var s = _stories[i];
                StoryList.Add(new StoryListItem { Index = i, TitleTr = s.TitleTr, Emoji = s.Emoji });
            }

            TotalStories = _stories.Count;
            if (_stories.Count > 0)
                LoadStory(0);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void SelectStory(StoryListItem item)
    {
        StopPlayback();
        LoadStory(item.Index);
    }

    [RelayCommand]
    public async Task PlayPauseAsync()
    {
        if (IsPlaying)
        {
            StopPlayback();
            return;
        }

        IsPlaying = true;
        IsFinished = false;
        _playCts = new CancellationTokenSource();
        var ct = _playCts.Token;

        try
        {
            for (int i = 0; i < Words.Count; i++)
            {
                if (ct.IsCancellationRequested) break;

                var word = Words[i];

                // Geçmiş kelimeleri işaretle
                for (int j = 0; j < i; j++) { Words[j].IsActive = false; Words[j].IsPast = true; }
                word.IsActive = true;
                word.IsPast = false;

                // Anahtar kelime ise ses çal
                if (word.IsKeyword && !string.IsNullOrWhiteSpace(word.ItemId))
                    await _audioService.PlayItemSoundAsync(word.ItemId);

                // Her kelime için gecikme (kelime uzunluğuna göre)
                int delayMs = word.IsKeyword ? 900 : 550;
                await Task.Delay(delayMs, ct).ContinueWith(_ => { });
            }

            if (!ct.IsCancellationRequested)
            {
                // Tüm kelimeler okundu
                foreach (var w in Words) { w.IsActive = false; w.IsPast = true; }
                IsFinished = true;
            }
        }
        finally
        {
            IsPlaying = false;
        }
    }

    [RelayCommand]
    public void ResetStory()
    {
        StopPlayback();
        LoadStory(CurrentStoryIndex);
    }

    [RelayCommand]
    public void NextStory()
    {
        StopPlayback();
        var next = (CurrentStoryIndex + 1) % _stories.Count;
        LoadStory(next);
    }

    [RelayCommand]
    public void PreviousStory()
    {
        StopPlayback();
        var prev = (CurrentStoryIndex - 1 + _stories.Count) % _stories.Count;
        LoadStory(prev);
    }

    [RelayCommand]
    public Task GoBackAsync()
    {
        StopPlayback();
        return _navigationService.GoBackAsync();
    }

    private void LoadStory(int index)
    {
        if (index < 0 || index >= _stories.Count) return;
        CurrentStoryIndex = index;
        var story = _stories[index];
        StoryTitle = story.TitleTr;
        StoryEmoji = story.CoverEmoji;
        IsFinished = false;

        foreach (var item in StoryList)
            item.IsSelected = item.Index == index;

        Words.Clear();
        foreach (var w in story.Words)
            Words.Add(new StoryWordItem
            {
                WordTr = w.WordTr,
                ItemId = w.ItemId,
                IsKeyword = w.IsKeyword
            });
    }

    private void StopPlayback()
    {
        _playCts?.Cancel();
        _playCts = null;
        IsPlaying = false;
    }
}

public partial class StoryListItem : ObservableObject
{
    public int Index { get; set; }
    public string TitleTr { get; set; } = string.Empty;
    public string Emoji { get; set; } = "📖";

    [ObservableProperty] private bool _isSelected;
    public string BackgroundColor => IsSelected ? "#EEF2FF" : "Transparent";
    public string BorderColor => IsSelected ? "#5148D4" : "Transparent";

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(BorderColor));
    }
}

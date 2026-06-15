using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Game;

public partial class ColoringGameViewModel : ObservableObject
{
    private readonly AudioService _audioService;
    private readonly NavigationService _navigationService;

    // Renk paleti
    public List<ColorChoice> Palette { get; } = new()
    {
        new("#EF4444", "Kırmızı"),
        new("#F97316", "Turuncu"),
        new("#EAB308", "Sarı"),
        new("#22C55E", "Yeşil"),
        new("#3B82F6", "Mavi"),
        new("#8B5CF6", "Mor"),
        new("#EC4899", "Pembe"),
        new("#6B7280", "Gri"),
        new("#92400E", "Kahve"),
        new("#FFFFFF", "Beyaz"),
    };

    [ObservableProperty] private ColorChoice? _selectedColor;
    [ObservableProperty] private List<ColorRegion> _regions = new();
    [ObservableProperty] private string _subjectEmoji = "🦁";
    [ObservableProperty] private string _subjectName = "Aslan";
    [ObservableProperty] private int _coloredCount;
    [ObservableProperty] private bool _isComplete;

    private readonly (string Emoji, string Name, string[] Parts)[] _subjectData = new[]
    {
        ("🦁", "Aslan",    new[] { "Yele", "Vücut", "Yüz", "Kulaklar", "Kuyruk" }),
        ("🦋", "Kelebek",  new[] { "Sol Kanat", "Sağ Kanat", "Gövde", "Anten" }),
        ("🌸", "Çiçek",    new[] { "Yaprak 1", "Yaprak 2", "Yaprak 3", "Yaprak 4", "Merkez", "Gövde" }),
        ("🐠", "Balık",    new[] { "Gövde", "Kanat", "Kuyruk", "Göz" }),
        ("🚂", "Tren",     new[] { "Kasa", "Pencere", "Tekerlekler", "Baca", "Ön" }),
    };

    public ColoringGameViewModel(AudioService audioService, NavigationService navigationService)
    {
        _audioService = audioService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public Task InitializeAsync()
    {
        var pick = _subjectData[Random.Shared.Next(_subjectData.Length)];
        SubjectEmoji = pick.Emoji;
        SubjectName = pick.Name;
        ColoredCount = 0;
        IsComplete = false;
        SelectedColor = Palette[0];

        Regions = pick.Parts.Select((part, i) => new ColorRegion
        {
            Id = i,
            Label = part,
        }).ToList();

        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task SelectColorAsync(ColorChoice color)
    {
        SelectedColor = color;
        await _audioService.PlayClickAsync();
        // Tüm renk seçimlerini güncelle
        foreach (var c in Palette)
            c.IsSelected = c == color;
    }

    [RelayCommand]
    public async Task ColorRegionAsync(ColorRegion region)
    {
        if (SelectedColor is null) return;
        if (region.FilledColor == SelectedColor.Hex) return;

        var wasColored = region.IsColored;
        region.FilledColor = SelectedColor.Hex;

        if (!wasColored)
        {
            ColoredCount++;
            await _audioService.PlayCorrectAsync();

            if (ColoredCount >= Regions.Count)
            {
                IsComplete = true;
                await _audioService.PlayCompleteAsync();
            }
        }
    }

    [RelayCommand]
    public Task ResetAsync()
    {
        foreach (var r in Regions)
            r.FilledColor = null;
        ColoredCount = 0;
        IsComplete = false;
        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task GoBackAsync() => await _navigationService.GoBackAsync();
}

public partial class ColorChoice : ObservableObject
{
    public string Hex { get; }
    public string Name { get; }
    [ObservableProperty] private bool _isSelected;

    public Color Color => Color.FromArgb(Hex);
    public Color BorderColor => IsSelected ? Color.FromArgb("#172033") : Color.FromArgb("#E8E8F0");
    public double BorderWidth => IsSelected ? 3 : 1.5;

    public ColorChoice(string hex, string name) { Hex = hex; Name = name; }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(IsSelected))
        {
            OnPropertyChanged(nameof(BorderColor));
            OnPropertyChanged(nameof(BorderWidth));
        }
    }
}

public partial class ColorRegion : ObservableObject
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;

    [ObservableProperty] private string? _filledColor;

    public bool IsColored => FilledColor is not null;

    public Color DisplayColor => FilledColor is not null
        ? Color.FromArgb(FilledColor)
        : Color.FromArgb("#E5E7EB");

    public Color TextColor => FilledColor is not null
        ? Colors.White
        : Color.FromArgb("#6B7280");

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(FilledColor))
        {
            OnPropertyChanged(nameof(IsColored));
            OnPropertyChanged(nameof(DisplayColor));
            OnPropertyChanged(nameof(TextColor));
        }
    }
}

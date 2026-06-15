using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Enums;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Game;

public partial class PuzzleGameViewModel : ObservableObject
{
    private readonly ContentService _contentService;
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;
    private readonly AudioService _audioService;

    private ChildProfile? _profile;
    private List<LearningItem> _puzzleItems = new();
    private PuzzlePiece? _selectedPiece;

    private const double PieceSize = 130;

    [ObservableProperty] private LearningItem? _currentItem;
    [ObservableProperty] private ObservableCollection<PuzzlePiece> _pieces = new();
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _isPuzzleSolved;
    [ObservableProperty] private int _swapCount;
    [ObservableProperty] private int _currentPuzzleIndex;
    [ObservableProperty] private int _totalPuzzles;
    [ObservableProperty] private string _promptText = "Parçaları doğru yerine koy!";
    private string _activeCategoryId = "animals";

    public double Progress => _totalPuzzles == 0 ? 0 : (double)_currentPuzzleIndex / _totalPuzzles;
    public int DisplayNumber => _currentPuzzleIndex + 1;

    public PuzzleGameViewModel(
        ContentService contentService,
        ProfileService profileService,
        NavigationService navigationService,
        AudioService audioService)
    {
        _contentService = contentService;
        _profileService = profileService;
        _navigationService = navigationService;
        _audioService = audioService;
    }

    [RelayCommand]
    public async Task InitializeAsync(string? categoryId = null)
    {
        IsLoading = true;
        try
        {
            _profile = _profileService.GetActiveProfile();
            if (_profile is null) return;

            _activeCategoryId = categoryId ?? "animals";
            var all = await _contentService.GetItemsAsync(_activeCategoryId);

            _puzzleItems = all
                .Where(i => !string.IsNullOrWhiteSpace(i.ImagePath))
                .OrderBy(_ => Guid.NewGuid())
                .Take(5)
                .ToList();

            if (_puzzleItems.Count == 0)
                _puzzleItems = await _contentService.GetMixedGameItemsAsync(_profile, 5);

            TotalPuzzles = _puzzleItems.Count;
            CurrentPuzzleIndex = 0;
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(DisplayNumber));

            LoadPuzzle(_puzzleItems[0]);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SelectPieceAsync(PuzzlePiece piece)
    {
        if (IsPuzzleSolved) return;

        if (_selectedPiece is null)
        {
            _selectedPiece = piece;
            piece.IsSelected = true;
            await _audioService.PlayClickAsync();
            return;
        }

        if (_selectedPiece == piece)
        {
            _selectedPiece.IsSelected = false;
            _selectedPiece = null;
            return;
        }

        var idx1 = Pieces.IndexOf(_selectedPiece);
        var idx2 = Pieces.IndexOf(piece);

        if (idx1 < 0 || idx2 < 0)
        {
            _selectedPiece.IsSelected = false;
            _selectedPiece = null;
            return;
        }

        _selectedPiece.IsSelected = false;
        Pieces[idx1] = piece;
        Pieces[idx2] = _selectedPiece;
        _selectedPiece = null;
        SwapCount++;

        await _audioService.PlayClickAsync();

        if (CheckSolved())
        {
            IsPuzzleSolved = true;
            PromptText = "🎉 Tebrikler! Puzzle tamamlandı!";
            await _audioService.PlayCompleteAsync();
            await Task.Delay(1800);

            CurrentPuzzleIndex++;
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(DisplayNumber));

            if (CurrentPuzzleIndex >= TotalPuzzles)
            {
                var session = new GameSession
                {
                    CategoryId = _activeCategoryId,
                    GameType = Enums.GameType.PuzzleSwap,
                    FinishedAt = DateTime.UtcNow,
                    Rounds = Enumerable.Range(0, TotalPuzzles).Select(_ => new GameRound
                    {
                        CorrectItem = new LearningItem(),
                        Result = Enums.GameResult.Correct
                    }).ToList()
                };
                if (_profile is not null)
                    _profileService.UpdateProgress(_profile.Id, _activeCategoryId, session);
                await _navigationService.GoToResultAsync(session);
                return;
            }

            IsPuzzleSolved = false;
            SwapCount = 0;
            LoadPuzzle(_puzzleItems[CurrentPuzzleIndex]);
        }
    }

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();

    private void LoadPuzzle(LearningItem item)
    {
        CurrentItem = item;
        PromptText = $"{item.NameTr} resmine dokunarak parçaları yerleştir!";
        _selectedPiece = null;

        var pieceList = new List<PuzzlePiece>
        {
            new() { CorrectIndex = 0, ImageSource = item.ImagePath, OffsetX = 0,          OffsetY = 0,          ItemNameTr = item.NameTr },
            new() { CorrectIndex = 1, ImageSource = item.ImagePath, OffsetX = -PieceSize,  OffsetY = 0,          ItemNameTr = item.NameTr },
            new() { CorrectIndex = 2, ImageSource = item.ImagePath, OffsetX = 0,          OffsetY = -PieceSize,  ItemNameTr = item.NameTr },
            new() { CorrectIndex = 3, ImageSource = item.ImagePath, OffsetX = -PieceSize,  OffsetY = -PieceSize,  ItemNameTr = item.NameTr },
        };

        // Shuffle: ensure not already in correct order
        List<PuzzlePiece> shuffled;
        do
        {
            shuffled = pieceList.OrderBy(_ => Guid.NewGuid()).ToList();
        }
        while (shuffled.Select((p, i) => p.CorrectIndex == i).All(x => x));

        Pieces = new ObservableCollection<PuzzlePiece>(shuffled);
    }

    private bool CheckSolved() =>
        Pieces.Select((p, i) => p.CorrectIndex == i).All(x => x);
}

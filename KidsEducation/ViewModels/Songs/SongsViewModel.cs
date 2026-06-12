using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Songs;

public partial class SongsViewModel : ObservableObject
{
    private readonly SongService _songService;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private List<SongItem> _songs = new();
    [ObservableProperty] private SongItem? _featuredSong;
    [ObservableProperty] private bool _isLoading = true;

    public int SongCount => Songs.Count;

    public SongsViewModel(SongService songService, NavigationService navigationService)
    {
        _songService = songService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            Songs = await _songService.GetSongsAsync();
            FeaturedSong = Songs.FirstOrDefault();
            OnPropertyChanged(nameof(SongCount));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SelectSongAsync(SongItem song)
    {
        if (song is null)
            return;

        await _navigationService.GoToSongDetailAsync(song.Id);
    }

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();
}

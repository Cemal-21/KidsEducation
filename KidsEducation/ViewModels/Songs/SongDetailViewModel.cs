using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;
using Plugin.Maui.Audio;

namespace KidsEducation.ViewModels.Songs;

[QueryProperty(nameof(SongId), "songId")]
public partial class SongDetailViewModel : ObservableObject
{
    private readonly SongService _songService;
    private readonly NavigationService _navigationService;
    private readonly IAudioManager _audioManager;
    private readonly AppPreferencesService _preferences;
    private IAudioPlayer? _player;
    private CancellationTokenSource? _positionTokenSource;

    [ObservableProperty] private string _songId = string.Empty;
    [ObservableProperty] private SongItem? _song;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private double _currentPosition;
    [ObservableProperty] private double _duration;
    [ObservableProperty] private string _loadErrorText = string.Empty;

    public string PlayButtonText => IsPlaying ? "Duraklat" : "Oynat";
    public string PlayButtonIcon => IsPlaying ? "⏸" : "▶";
    public double Progress => Duration <= 0 ? 0 : Math.Clamp(CurrentPosition / Duration, 0, 1);
    public string CurrentTimeText => FormatTime(CurrentPosition);
    public string DurationTimeText => Duration > 0 ? FormatTime(Duration) : Song?.DurationText ?? "--:--";
    public bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorText);

    public SongDetailViewModel(
        SongService songService,
        NavigationService navigationService,
        IAudioManager audioManager,
        AppPreferencesService preferences)
    {
        _songService = songService;
        _navigationService = navigationService;
        _audioManager = audioManager;
        _preferences = preferences;
    }

    partial void OnIsPlayingChanged(bool value)
    {
        OnPropertyChanged(nameof(PlayButtonText));
        OnPropertyChanged(nameof(PlayButtonIcon));
    }

    partial void OnCurrentPositionChanged(double value)
    {
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(CurrentTimeText));
    }

    partial void OnDurationChanged(double value)
    {
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(DurationTimeText));
    }

    partial void OnLoadErrorTextChanged(string value) =>
        OnPropertyChanged(nameof(HasLoadError));

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(SongId))
            return;

        StopSong();
        Song = await _songService.GetSongAsync(SongId);
        OnPropertyChanged(nameof(DurationTimeText));
    }

    [RelayCommand]
    public async Task PlayPauseAsync()
    {
        if (IsPlaying)
        {
            PauseSong();
            return;
        }

        if (Song is null || string.IsNullOrWhiteSpace(Song.AudioFile))
            return;

        try
        {
            LoadErrorText = string.Empty;
            await EnsurePlayerAsync();

            if (_player is null)
                return;

            _player.Volume = _preferences.MasterVolume;
            _player.Play();
            IsPlaying = true;
            StartPositionTracking();
        }
        catch (Exception ex)
        {
            LoadErrorText = ex switch
            {
                FileNotFoundException => "Şarkı dosyası bulunamadı. MP3 dosyasını Audio klasörüne ekleyip tekrar dene.",
                InvalidDataException => "Şarkı dosyası geçerli bir MP3 değil. Dosyayı yeniden indirip Audio klasörüne ekle.",
                _ => "Şarkı dosyası oynatılamadı. MP3 dosyasını kontrol edip tekrar dene."
            };
            System.Diagnostics.Debug.WriteLine($"[SongDetail] {Song.AudioFile} oynatılamadı: {ex.Message}");
            IsPlaying = false;
        }
    }

    [RelayCommand]
    public Task GoBackAsync()
    {
        StopSong();
        return _navigationService.GoBackOneAsync();
    }

    public void StopSong()
    {
        StopPositionTracking();

        try
        {
            _player?.Stop();
            _player = null;
        }
        catch
        {
            _player = null;
        }

        CurrentPosition = 0;
        IsPlaying = false;
    }

    private void PauseSong()
    {
        StopPositionTracking();

        try
        {
            _player?.Pause();
        }
        catch
        {
        }

        IsPlaying = false;
    }

    private async Task EnsurePlayerAsync()
    {
        if (_player is not null)
            return;

        if (Song is null)
            return;

        var stream = await OpenSongStreamAsync(Song.AudioFile);
        await EnsurePlayableMp3Async(stream);

        _player = _audioManager.CreatePlayer(stream);
        _player.Volume = _preferences.MasterVolume;
        Duration = _player.Duration;
        _player.PlaybackEnded += OnPlaybackEnded;
    }

    private static async Task EnsurePlayableMp3Async(Stream stream)
    {
        if (!stream.CanSeek)
            return;

        var originalPosition = stream.Position;
        var header = new byte[16];
        var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length));
        stream.Position = originalPosition;

        if (bytesRead < 3)
            throw new InvalidDataException("Dosya MP3 başlığı içermiyor.");

        var startsWithId3 = header[0] == 'I' && header[1] == 'D' && header[2] == '3';
        var startsWithMp3Frame = header[0] == 0xFF && (header[1] & 0xE0) == 0xE0;
        var startsWithHtml = bytesRead >= 5 &&
            header[0] == '<' &&
            header[1] == '!' &&
            header[2] == 'D' &&
            header[3] == 'O' &&
            header[4] == 'C';

        if (startsWithHtml || (!startsWithId3 && !startsWithMp3Frame))
            throw new InvalidDataException("Dosya MP3 yerine farklı bir içerik taşıyor.");
    }

    private static async Task<Stream> OpenSongStreamAsync(string audioFile)
    {
        var fileName = audioFile
            .Replace("\\", "/", StringComparison.Ordinal)
            .TrimStart('/');

        var candidates = fileName.StartsWith("Audio/", StringComparison.OrdinalIgnoreCase)
            ? new[] { fileName, fileName.Substring("Audio/".Length) }
            : new[] { $"Audio/{fileName}", fileName };

        Exception? lastException = null;

        foreach (var candidate in candidates)
        {
            try
            {
                return await FileSystem.OpenAppPackageFileAsync(candidate);
            }
            catch (Exception ex)
            {
                lastException = ex;
                System.Diagnostics.Debug.WriteLine($"[SongDetail] Paket dosyası denenemedi: {candidate} - {ex.Message}");
            }
        }

        throw lastException ?? new FileNotFoundException(fileName);
    }

    private void StartPositionTracking()
    {
        StopPositionTracking();
        _positionTokenSource = new CancellationTokenSource();
        _ = TrackPositionAsync(_positionTokenSource.Token);
    }

    private void StopPositionTracking()
    {
        _positionTokenSource?.Cancel();
        _positionTokenSource?.Dispose();
        _positionTokenSource = null;
    }

    private async Task TrackPositionAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && _player is not null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    CurrentPosition = _player?.CurrentPosition ?? 0;
                    Duration = _player?.Duration ?? Duration;
                });

                await Task.Delay(180, token);
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StopPositionTracking();
            IsPlaying = false;
            CurrentPosition = 0;
        });
    }

    private static string FormatTime(double seconds)
    {
        if (seconds < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds))
            seconds = 0;

        var time = TimeSpan.FromSeconds(seconds);
        return $"{(int)time.TotalMinutes}:{time.Seconds:00}";
    }
}

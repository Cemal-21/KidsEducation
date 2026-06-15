using Plugin.Maui.Audio;

namespace KidsEducation.Services;

public class AudioService
{
    private readonly IAudioManager _audioManager;
    private readonly AppPreferencesService _preferences;
    private IAudioPlayer? _speechPlayer;
    private IAudioPlayer? _musicPlayer;
    private readonly List<IAudioPlayer> _effectPlayers = new();
    private readonly object _effectLock = new();
    private string? _currentMusicFile;

    public AudioService(IAudioManager audioManager, AppPreferencesService preferences)
    {
        _audioManager = audioManager;
        _preferences = preferences;
    }

    // ── Arka plan müziği ──────────────────────────────────────

    public async Task StartBackgroundMusicAsync(string fileName = "music_background.mp3")
    {
        try
        {
            if (!_preferences.MusicEnabled) return;
            if (_currentMusicFile == fileName && _musicPlayer?.IsPlaying == true) return;

            StopBackgroundMusic();
            var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            _musicPlayer = _audioManager.CreatePlayer(stream);
            _musicPlayer.Volume = _preferences.MusicVolume;
            _musicPlayer.Loop = true;
            _musicPlayer.Play();
            _currentMusicFile = fileName;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] Müzik başlatılamadı: {ex.Message}");
        }
    }

    public void StopBackgroundMusic()
    {
        try
        {
            _musicPlayer?.Stop();
            if (_musicPlayer is IDisposable d) d.Dispose();
        }
        catch { }
        finally
        {
            _musicPlayer = null;
            _currentMusicFile = null;
        }
    }

    public void SetMusicEnabled(bool enabled)
    {
        _preferences.MusicEnabled = enabled;
        if (enabled)
            _ = StartBackgroundMusicAsync();
        else
            StopBackgroundMusic();
    }

    public void SetMusicVolume(double volume)
    {
        _preferences.MusicVolume = volume;
        if (_musicPlayer is not null)
            _musicPlayer.Volume = volume;
    }

    public void SetEffectsEnabled(bool enabled) =>
        _preferences.EffectsEnabled = enabled;

    public void SetEffectsVolume(double volume) =>
        _preferences.MasterVolume = Math.Clamp(volume, 0, 1);

    public bool MusicEnabled => _preferences.MusicEnabled;
    public bool EffectsEnabled => _preferences.EffectsEnabled;
    public double MusicVolume => _preferences.MusicVolume;
    public double EffectsVolume => _preferences.MasterVolume;

    // ── Oyun ses efektleri ────────────────────────────────────

    public async Task PlayCorrectAsync() => _ = await TryPlayFileAsync("sound_correct.mp3");
    public async Task PlayWrongAsync() => _ = await TryPlayFileAsync("sound_wrong.mp3");
    public async Task PlayStarAsync() => _ = await TryPlayFileAsync("sound_star.mp3");
    public async Task PlayCompleteAsync() => _ = await TryPlayFileAsync("sound_complete.mp3");
    public async Task PlayClickAsync() => _ = await TryPlayFileAsync("sound_click.mp3");
    public async Task PlayFailAsync() => _ = await TryPlayFileAsync("sound_fail.mp3");

    // ── İsim sesi: Audio/speech_tr_kedi.mp3 ──────────────────
    public async Task PlayItemSoundAsync(string itemId, bool? male = null)
    {
        var key = itemId.Contains('_') ? itemId.Substring(itemId.IndexOf('_') + 1) : itemId;
        var suffix = (male ?? _preferences.IsMaleVoice) ? "_m" : "";
        var fileName = $"Audio/speech_tr_{key}{suffix}.mp3";
        await PlaySpeechAsync(fileName);
    }

    // ── Sesli ipucu: Audio/clue_animal_kedi.mp3 ──────────────
    public async Task PlaySoundClueAsync(string itemId, bool? male = null)
    {
        var suffix = (male ?? _preferences.IsMaleVoice) ? "_m" : "";
        await PlaySpeechAsync($"Audio/clue_{itemId}{suffix}.mp3");
    }

    // ── Açıklama: Audio/animal_kedi.mp3 ──────────────────────
    public async Task SpeakDescriptionAsync(string itemId)
    {
        await PlaySpeechAsync($"Audio/{itemId}.mp3");
    }

    // ── FunFact: Audio/fact_animal_kedi.mp3 ──────────────────
    public async Task SpeakFunFactAsync(string itemId)
    {
        await PlaySpeechAsync($"Audio/fact_{itemId}.mp3");
    }

    // ── UI sesleri: Audio/speech_tr_aferin.mp3 ───────────────
    public async Task SpeakUIAsync(string key, bool? male = null)
    {
        var suffix = (male ?? _preferences.IsMaleVoice) ? "_m" : "";
        await PlaySpeechAsync($"Audio/speech_tr_{key}{suffix}.mp3");
    }

    // ── Cihaz TTS (metin okuma) ───────────────────────────────
    public async Task SpeakTextAsync(string text)
    {
        if (!_preferences.EffectsEnabled || string.IsNullOrWhiteSpace(text)) return;
        try
        {
            StopSpeech();
            await TextToSpeech.SpeakAsync(text, new SpeechOptions
            {
                Locale = await GetTurkishLocaleAsync(),
                Pitch = (float)(1.1 * _preferences.SpeechRate),
                Volume = (float)_preferences.MasterVolume
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] TTS hatası: {ex.Message}");
        }
    }

    private static Locale? _turkishLocale;
    private static async Task<Locale?> GetTurkishLocaleAsync()
    {
        if (_turkishLocale is not null) return _turkishLocale;
        var locales = await TextToSpeech.GetLocalesAsync();
        _turkishLocale = locales.FirstOrDefault(l =>
            l.Language.StartsWith("tr", StringComparison.OrdinalIgnoreCase));
        return _turkishLocale;
    }

    public void StopSpeech()
    {
        try
        {
            _speechPlayer?.Stop();

            if (_speechPlayer is IDisposable disposable)
                disposable.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] Konuşma durdurulamadı: {ex.Message}");
        }
        finally
        {
            _speechPlayer = null;
        }
    }

    // ── Yardımcı metodlar ─────────────────────────────────────

    public async Task PlayFileAsync(string fileName) => await TryPlayFileAsync(fileName);

    public async Task<bool> TryPlayFileAsync(string fileName)
    {
        if (await PlayAsync(fileName))
            return true;

        if (!fileName.Contains('/') && !fileName.Contains('\\'))
            return await PlayAsync($"Audio/{fileName}");

        return false;
    }

    private async Task<bool> PlayAsync(string fileName)
    {
        if (!_preferences.EffectsEnabled)
            return false;

        try
        {
            var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            var player = _audioManager.CreatePlayer(stream);
            player.Volume = _preferences.MasterVolume;
            player.PlaybackEnded += OnEffectPlaybackEnded;
            lock (_effectLock)
                _effectPlayers.Add(player);
            player.Play();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] {fileName} oynatılamadı: {ex.Message}");
            return false;
        }
    }

    private void OnEffectPlaybackEnded(object? sender, EventArgs e)
    {
        if (sender is not IAudioPlayer player) return;

        player.PlaybackEnded -= OnEffectPlaybackEnded;

        bool removed;
        lock (_effectLock)
            removed = _effectPlayers.Remove(player);

        if (!removed) return;

        _ = DisposeEffectPlayerLaterAsync(player);
    }

    private static async Task DisposeEffectPlayerLaterAsync(IAudioPlayer player)
    {
        await Task.Delay(1500);
        try
        {
            if (player is IDisposable disposable)
                disposable.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] Player dispose hatası: {ex.Message}");
        }
    }
    public async Task SpeakAsync(string key, bool male = false)
    => await SpeakUIAsync(key, male);
    private async Task PlaySpeechAsync(string fileName)
    {
        System.Diagnostics.Debug.WriteLine($"[AudioService] Trying to play: {fileName}");
        StopSpeech();
        try
        {
            var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            _speechPlayer = _audioManager.CreatePlayer(stream);
            _speechPlayer.Volume = _preferences.MasterVolume;
            _speechPlayer.Play();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] {fileName} oynatılamadı: {ex.Message}");
        }
    }
}

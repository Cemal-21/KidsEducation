using Plugin.Maui.Audio;

namespace KidsEducation.Services;

public class AudioService
{
    private readonly IAudioManager _audioManager;
    private readonly AppPreferencesService _preferences;
    private IAudioPlayer? _speechPlayer;

    public AudioService(IAudioManager audioManager, AppPreferencesService preferences)
    {
        _audioManager = audioManager;
        _preferences = preferences;
    }

    // ── Oyun ses efektleri ────────────────────────────────────

    public async Task PlayCorrectAsync() => await PlayAsync("sound_correct.mp3");
    public async Task PlayWrongAsync() => await PlayAsync("sound_wrong.mp3");
    public async Task PlayStarAsync() => await PlayAsync("sound_star.mp3");
    public async Task PlayCompleteAsync() => await PlayAsync("sound_complete.mp3");
    public async Task PlayClickAsync() => await PlayAsync("sound_click.mp3");
    public async Task PlayFailAsync() => await PlayAsync("sound_fail.mp3");

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

    private async Task PlayAsync(string fileName)
    {
        if (!_preferences.EffectsEnabled)
            return;

        try
        {
            var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            var player = _audioManager.CreatePlayer(stream);
            player.Volume = _preferences.MasterVolume;
            player.Play();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] {fileName} oynatılamadı: {ex.Message}");
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

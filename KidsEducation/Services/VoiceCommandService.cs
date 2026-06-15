using System.Globalization;
using CommunityToolkit.Maui.Media;

namespace KidsEducation.Services;

public class VoiceCommandService
{
    private readonly ISpeechToText _stt;
    private readonly NavigationService _nav;

    private static readonly (string[] Keywords, string Route, Func<VoiceCommandService, Task> Action)[] Commands =
    {
        (new[] { "hayvanlar", "hayvan" }, "animals", s => s._nav.GoToCategoryAsync("animals")),
        (new[] { "meyveler", "meyve" }, "fruits", s => s._nav.GoToCategoryAsync("fruits")),
        (new[] { "sebzeler", "sebze" }, "vegetables", s => s._nav.GoToCategoryAsync("vegetables")),
        (new[] { "renkler", "renk" }, "colors", s => s._nav.GoToCategoryAsync("colors")),
        (new[] { "sekiller", "sekil", "şekiller", "şekil" }, "shapes", s => s._nav.GoToCategoryAsync("shapes")),
        (new[] { "araclar", "arac", "araçlar", "araç", "tasit", "taşıt" }, "vehicles", s => s._nav.GoToCategoryAsync("vehicles")),
        (new[] { "sayilar", "sayi", "sayılar", "sayı" }, "numbers", s => s._nav.GoToCategoryAsync("numbers")),
        (new[] { "harfler", "harf" }, "letters", s => s._nav.GoToCategoryAsync("letters")),
        (new[] { "duygular", "duygu" }, "emotions", s => s._nav.GoToCategoryAsync("emotions")),
        (new[] { "gezegenler", "gezegen" }, "planets", s => s._nav.GoToCategoryAsync("planets")),
        (new[] { "sehirler", "sehir", "şehirler", "şehir", "iller", "il" }, "cities", s => s._nav.GoToCategoryAsync("cities")),
        (new[] { "ulkeler", "ulke", "ülkeler", "ülke" }, "countries", s => s._nav.GoToCategoryAsync("countries")),
        (new[] { "meslekler", "meslek" }, "professions", s => s._nav.GoToCategoryAsync("professions")),
        (new[] { "doga", "doğa" }, "nature", s => s._nav.GoToCategoryAsync("nature")),
        (new[] { "nesneler", "nesne" }, "objects", s => s._nav.GoToCategoryAsync("objects")),
        (new[] { "zitlar", "zit", "zıtlar", "zıt", "karsit", "karşıt" }, "opposites", s => s._nav.GoToCategoryAsync("opposites")),
        (new[] { "oyun", "oyunlar", "oyna" }, "__games", s => s._nav.GoToGamesAsync()),
        (new[] { "sarki", "sarkilar", "şarkı", "şarkılar", "muzik", "müzik" }, "__songs", s => s._nav.GoToSongsAsync()),
        (new[] { "macera", "harita" }, "__adventure", s => s._nav.GoToAdventureMapAsync()),
        (new[] { "ana sayfa", "ana", "eve don", "eve dön" }, "__home", _ => Shell.Current.GoToAsync("//home")),
        (new[] { "geri", "geri don", "geri dön" }, "__back", _ => Shell.Current.GoToAsync("..")),
        (new[] { "ebeveyn", "veli", "ayarlar", "ayar" }, "__parental", s => s._nav.GoToParentalAsync()),
        (new[] { "konular", "konu", "ogren", "öğren" }, "__topics", _ => Shell.Current.GoToAsync("learningmodules")),
        (new[] { "nokta", "nokta birlestir", "nokta birleştir" }, "__connectdots", _ => Shell.Current.GoToAsync("connectdots")),
        (new[] { "cizim", "çizim", "ciz", "çiz" }, "__drawing", _ => Shell.Current.GoToAsync("drawinggame"))
    };

    public VoiceCommandService(ISpeechToText stt, NavigationService nav, AudioService audio)
    {
        _stt = stt;
        _nav = nav;
    }

    public async Task<VoiceCommandResult> ListenAndExecuteAsync(CancellationToken ct = default)
    {
        string recognized = "";
        try
        {
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            void OnCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs e) =>
                tcs.TrySetResult(e.RecognitionResult.Text ?? "");

            _stt.RecognitionResultCompleted += OnCompleted;
            try
            {
                var options = new SpeechToTextOptions
                {
                    Culture = CultureInfo.GetCultureInfo("tr-TR"),
                    ShouldReportPartialResults = false
                };

                await _stt.StartListenAsync(options, ct);
                using var reg = ct.Register(() => tcs.TrySetCanceled());
                recognized = await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                _stt.RecognitionResultCompleted -= OnCompleted;
                try { await _stt.StopListenAsync(CancellationToken.None); } catch { }
            }
        }
        catch (OperationCanceledException)
        {
            return new VoiceCommandResult(recognized, false, null, null);
        }
        catch
        {
            return new VoiceCommandResult("", false, null, null);
        }

        if (string.IsNullOrWhiteSpace(recognized))
            return new VoiceCommandResult("", false, null, null);

        var normalized = NormalizeCommand(recognized);

        foreach (var (keywords, route, action) in Commands)
        {
            if (keywords.Any(k => normalized.Contains(NormalizeCommand(k))))
            {
                await action(this);
                return new VoiceCommandResult(recognized, true, keywords[0], route);
            }
        }

        return new VoiceCommandResult(recognized, false, null, null);
    }

    public Task GoToCategoryAsync(string id) => _nav.GoToCategoryAsync(id);

    private static string NormalizeCommand(string text)
    {
        return text.Trim()
            .ToLower(new CultureInfo("tr-TR"))
            .Replace('ı', 'i')
            .Replace('ğ', 'g')
            .Replace('ü', 'u')
            .Replace('ş', 's')
            .Replace('ö', 'o')
            .Replace('ç', 'c');
    }
}

public record VoiceCommandResult(
    string RecognizedText,
    bool CommandFound,
    string? MatchedKeyword,
    string? MatchedRoute);

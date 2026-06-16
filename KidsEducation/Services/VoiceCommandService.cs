using System.Globalization;
using System.Text;
using CommunityToolkit.Maui.Media;

namespace KidsEducation.Services;

public class VoiceCommandService
{
    private readonly ISpeechToText _speechToText;

    private static readonly CommandDefinition[] Commands =
    [
        new(["hayvanlar", "hayvan"], "animals", "Hayvanlar"),
        new(["meyveler", "meyve"], "fruits", "Meyveler"),
        new(["sebzeler", "sebze"], "vegetables", "Sebzeler"),
        new(["renkler", "renk"], "colors", "Renkler"),
        new(["sekiller", "sekil", "shapes"], "shapes", "Sekiller"),
        new(["araclar", "arac", "tasitlar", "tasit"], "vehicles", "Araclar"),
        new(["sayilar", "sayi"], "numbers", "Sayilar"),
        new(["harfler", "harf"], "letters", "Harfler"),
        new(["duygular", "duygu"], "emotions", "Duygular"),
        new(["gezegenler", "gezegen"], "planets", "Gezegenler"),
        new(["sehirler", "sehir", "iller", "il"], "cities", "Sehirler"),
        new(["ulkeler", "ulke"], "countries", "Ulkeler"),
        new(["meslekler", "meslek"], "professions", "Meslekler"),
        new(["doga"], "nature", "Doga"),
        new(["nesneler", "nesne", "esyalar", "esya"], "objects", "Esyalar"),
        new(["zitlar", "zit", "karsit"], "opposites", "Zitlar"),
        new(["trafik", "trafik isaretleri"], "traffic", "Trafik"),
        new(["hava", "hava durumu"], "weather", "Hava Durumu"),
        new(["mevsimler", "mevsim"], "seasons", "Mevsimler"),
        new(["vucudum", "vucut"], "body", "Vucudum"),
        new(["oyun", "oyunlar", "oyna"], "__games", "Oyunlar"),
        new(["gunluk gorev", "gunluk plan", "hedef"], "__dailygoal", "Gunluk Gorevler"),
        new(["ilerleme", "rapor"], "__progress", "Ilerleme"),
        new(["basari", "basarilar", "basarim"], "__achievements", "Basarimlar"),
        new(["masal", "masallar"], "__tales", "Masallar"),
        new(["sarki", "sarkilar", "muzik"], "__songs", "Sarkilar"),
        new(["macera", "harita"], "__adventure", "Macera"),
        new(["ana sayfa", "ana", "ev"], "__home", "Ana Sayfa"),
        new(["geri", "geri don"], "__back", "Geri"),
        new(["ebeveyn", "veli", "ayarlar", "ayar"], "__parental", "Ebeveyn"),
        new(["konular", "konu", "ogren"], "__topics", "Konular"),
        new(["nokta", "nokta birlestir"], "__connectdots", "Nokta Birlestir"),
        new(["cizim", "ciz", "sekil ciz"], "__drawing", "Cizim"),
        new(["boyama", "renk boya", "resim boya"], "__coloring", "Boyama"),
        new(["sekil boyama"], "__shapecoloring", "Sekil Boyama"),
        new(["matematik", "toplama", "cikarma"], "__math", "Matematik"),
        new(["eslestirme", "eslestir"], "__matching", "Eslestirme"),
        new(["hafiza", "kart"], "__memory", "Hafiza"),
        new(["ses oyunu", "sesli tahmin", "ses"], "__sound", "Sesli Tahmin"),
    ];

    public VoiceCommandService(ISpeechToText speechToText, NavigationService nav, AudioService audio)
    {
        _speechToText = speechToText;
    }

    public async Task<VoiceCommandResult> ListenAsync(CancellationToken cancellationToken = default)
    {
        string recognizedText = "";
        EventHandler<SpeechToTextRecognitionResultCompletedEventArgs>? completed = null;

        try
        {
            var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            completed = (_, e) => completion.TrySetResult(e.RecognitionResult.Text ?? "");
            _speechToText.RecognitionResultCompleted += completed;

            var options = new SpeechToTextOptions
            {
                Culture = CultureInfo.GetCultureInfo("tr-TR"),
                ShouldReportPartialResults = false
            };

            await InvokeOnMainThreadAsync(() => _speechToText.StartListenAsync(options, cancellationToken));
            using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
            recognizedText = await completion.Task;
        }
        catch (OperationCanceledException)
        {
            return new VoiceCommandResult(recognizedText, false, null, null);
        }
        catch
        {
            return new VoiceCommandResult("", false, null, null);
        }
        finally
        {
            if (completed is not null)
                _speechToText.RecognitionResultCompleted -= completed;

            try
            {
                await InvokeOnMainThreadAsync(() => _speechToText.StopListenAsync(CancellationToken.None));
            }
            catch
            {
            }
        }

        if (string.IsNullOrWhiteSpace(recognizedText))
            return new VoiceCommandResult("", false, null, null);

        var normalized = NormalizeCommand(recognizedText);
        foreach (var command in Commands)
        {
            var matched = command.Keywords.FirstOrDefault(keyword => normalized.Contains(NormalizeCommand(keyword)));
            if (matched is not null)
                return new VoiceCommandResult(recognizedText, true, matched, command.Route, false, command.DisplayName);
        }

        var fallback = GetBestFallback(normalized);
        return fallback is null
            ? new VoiceCommandResult(recognizedText, false, null, null)
            : new VoiceCommandResult(recognizedText, false, fallback.Keyword, fallback.Route, true, fallback.DisplayName);
    }

    private static Task InvokeOnMainThreadAsync(Func<Task> action)
    {
        if (MainThread.IsMainThread)
            return action();

        return MainThread.InvokeOnMainThreadAsync(action);
    }

    public static string NormalizeCommand(string text)
    {
        var lower = text.Trim().ToLower(new CultureInfo("tr-TR"));
        var normalized = lower.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;

            builder.Append(ch switch
            {
                'ı' => 'i',
                'ğ' => 'g',
                'ü' => 'u',
                'ş' => 's',
                'ö' => 'o',
                'ç' => 'c',
                _ => ch
            });
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static FallbackMatch? GetBestFallback(string normalizedText)
    {
        var tokens = normalizedText
            .Split([' ', ',', '.', '!', '?'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length == 0)
            return null;

        FallbackMatch? best = null;
        foreach (var command in Commands)
        {
            foreach (var keyword in command.Keywords)
            {
                var normalizedKeyword = NormalizeCommand(keyword);
                var keywordTokens = normalizedKeyword
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                var overlapScore = keywordTokens.Count(token => tokens.Contains(token));
                var containsScore = normalizedKeyword.Contains(normalizedText) || normalizedText.Contains(normalizedKeyword) ? 2 : 0;
                var distance = LevenshteinDistance(normalizedText, normalizedKeyword);
                var score = (overlapScore * 3) + containsScore - distance;

                if (score < 1)
                    continue;

                if (best is null || score > best.Score)
                    best = new FallbackMatch(command.Route, command.DisplayName, keyword, score);
            }
        }

        return best is { Score: >= 2 } ? best : null;
    }

    private static int LevenshteinDistance(string source, string target)
    {
        if (source == target)
            return 0;

        if (source.Length == 0)
            return target.Length;

        if (target.Length == 0)
            return source.Length;

        var matrix = new int[source.Length + 1, target.Length + 1];

        for (var i = 0; i <= source.Length; i++)
            matrix[i, 0] = i;

        for (var j = 0; j <= target.Length; j++)
            matrix[0, j] = j;

        for (var i = 1; i <= source.Length; i++)
        {
            for (var j = 1; j <= target.Length; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[source.Length, target.Length];
    }

    private sealed record CommandDefinition(string[] Keywords, string Route, string DisplayName);
    private sealed record FallbackMatch(string Route, string DisplayName, string Keyword, int Score);
}

public record VoiceCommandResult(
    string RecognizedText,
    bool CommandFound,
    string? MatchedKeyword,
    string? MatchedRoute,
    bool HasFallbackSuggestion = false,
    string? SuggestedDisplayName = null);

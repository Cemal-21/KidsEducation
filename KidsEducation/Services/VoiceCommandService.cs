using CommunityToolkit.Maui.Media;

namespace KidsEducation.Services;

public class VoiceCommandService
{
    private readonly ISpeechToText _stt;
    private readonly NavigationService _nav;
    private readonly AudioService _audio;

    private static readonly Dictionary<string[], Func<VoiceCommandService, Task>> Commands = new()
    {
        { new[] { "hayvanlar", "hayvan" },          s => s._nav.GoToCategoryAsync("animals")     },
        { new[] { "meyveler", "meyve" },            s => s._nav.GoToCategoryAsync("fruits")      },
        { new[] { "sebzeler", "sebze" },            s => s._nav.GoToCategoryAsync("vegetables")  },
        { new[] { "renkler", "renk" },              s => s._nav.GoToCategoryAsync("colors")      },
        { new[] { "şekiller", "şekil" },            s => s._nav.GoToCategoryAsync("shapes")      },
        { new[] { "araçlar", "araç" },              s => s._nav.GoToCategoryAsync("vehicles")    },
        { new[] { "sayılar", "sayı" },              s => s._nav.GoToCategoryAsync("numbers")     },
        { new[] { "harfler", "harf" },              s => s._nav.GoToCategoryAsync("letters")     },
        { new[] { "duygular", "duygu" },            s => s._nav.GoToCategoryAsync("emotions")    },
        { new[] { "gezegenler", "gezegen" },        s => s._nav.GoToCategoryAsync("planets")     },
        { new[] { "oyun", "oyunlar", "oyna" },      s => s._nav.GoToGamesAsync()                 },
        { new[] { "şarkı", "şarkılar", "müzik" },  s => s._nav.GoToSongsAsync()                 },
        { new[] { "macera", "harita" },             s => s._nav.GoToAdventureMapAsync()          },
        { new[] { "ana sayfa", "ana", "eve dön" },  s => Shell.Current.GoToAsync("//home")       },
        { new[] { "geri", "geri dön" },             s => Shell.Current.GoToAsync("..")           },
        { new[] { "ebeveyn", "veli", "ayarlar" },   s => s._nav.GoToParentalAsync()              },
        { new[] { "konular", "öğren" },             s => Shell.Current.GoToAsync("learningmodules") },
        { new[] { "nokta", "nokta birleştir" },     s => Shell.Current.GoToAsync("connectdots")  },
        { new[] { "çizim", "çiz" },                 s => Shell.Current.GoToAsync("drawinggame")  },
    };

    public VoiceCommandService(ISpeechToText stt, NavigationService nav, AudioService audio)
    {
        _stt = stt;
        _nav = nav;
        _audio = audio;
    }

    /// <summary>
    /// Dinlemeyi başlatır. Tanınan metni döndürür, komut varsa çalıştırır.
    /// </summary>
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
                    Culture = System.Globalization.CultureInfo.GetCultureInfo("tr-TR"),
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
        catch (OperationCanceledException) { return new VoiceCommandResult(recognized, false, null); }
        catch { return new VoiceCommandResult("", false, null); }

        if (string.IsNullOrWhiteSpace(recognized))
            return new VoiceCommandResult("", false, null);

        var lower = recognized.ToLowerInvariant().Trim();

        foreach (var (keywords, action) in Commands)
        {
            if (keywords.Any(k => lower.Contains(k)))
            {
                await action(this);
                return new VoiceCommandResult(recognized, true, keywords[0]);
            }
        }

        return new VoiceCommandResult(recognized, false, null);
    }

    public Task GoToCategoryAsync(string id) => _nav.GoToCategoryAsync(id);
}

public record VoiceCommandResult(string RecognizedText, bool CommandFound, string? MatchedKeyword);

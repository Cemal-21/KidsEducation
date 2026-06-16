using Plugin.LocalNotification;

namespace KidsEducation.Services;

public class NotificationService
{
    private const int DailyReminderNotificationId = 1001;
    private const int CoachNotificationId = 2001;
    private const string NotificationEnabledKey = "notif_daily_enabled";
    private const string NotificationHourKey = "notif_daily_hour";
    private const string NotificationMinuteKey = "notif_daily_minute";
    private const string NotificationInitializedKey = "notif_daily_initialized";

    private static readonly string[] WarmTitles =
    {
        "Kisa bir ogrenme molasi",
        "Baykus seni bekliyor",
        "Bugunun mini gorevi hazir",
        "Bir oyunluk macera zamani"
    };

    private static readonly string[] StarterBodies =
    {
        "Bugun kisa bir oyunla ritmi yeniden yakalayabiliriz.",
        "Hazirsan bugunu minik bir ogrenme molasiyla acabiliriz.",
        "Birlikte 3-4 dakikalik kucuk bir tur iyi gelebilir."
    };

    private static readonly string[] StreakBodies =
    {
        "{0} mini gorev kaldi. Kisa bir oyunla serin devam etsin.",
        "Serin cok guzel gidiyor. Kalan {0} mini gorevi birlikte kapatabiliriz.",
        "Bugunku ritmi kacirmayalim. {0} mini gorev daha seni bekliyor."
    };

    private static readonly string[] NearlyDoneBodies =
    {
        "Son mini gorev kaldi. Hemen tamamlayabiliriz.",
        "Bugunun plani bitmeye cok yakin. Son bir adim kaldi.",
        "Sadece bir gorev daha ve bugun tamam."
    };

    private static readonly string[] CompletedBodies =
    {
        "Bugunun hedefleri tamam. Yarin yeni mini gorevler seni bekliyor.",
        "Harika is cikardin. Bugunluk plan kapanmis gorunuyor.",
        "Bugunku tum mini gorevleri bitirdin. Biraz sonra yine devam edebiliriz."
    };

    private readonly ProfileService _profileService;

    public NotificationService(ProfileService profileService)
    {
        _profileService = profileService;
    }

    public async Task RequestPermissionAsync()
    {
        await LocalNotificationCenter.Current.RequestNotificationPermission();
    }

    public async Task EnsureDefaultDailyReminderAsync()
    {
        if (!Preferences.ContainsKey(NotificationInitializedKey))
        {
            Preferences.Set(NotificationInitializedKey, true);
            Preferences.Set(NotificationEnabledKey, true);
            Preferences.Set(NotificationHourKey, 18);
            Preferences.Set(NotificationMinuteKey, 30);
        }

        if (!Preferences.Get(NotificationEnabledKey, true))
            return;

        await RequestPermissionAsync();
        await ScheduleDailyReminderAsync(new TimeSpan(
            Preferences.Get(NotificationHourKey, 18),
            Preferences.Get(NotificationMinuteKey, 30),
            0));
    }

    public async Task ScheduleDailyReminderAsync(TimeSpan time)
    {
        await CancelDailyReminderAsync();

        var now = DateTime.Now;
        var scheduledTime = DateTime.Today.Add(time);
        if (scheduledTime <= now)
            scheduledTime = scheduledTime.AddDays(1);

        var profile = _profileService.GetActiveProfile();
        var message = BuildReminderMessage(profile);

        var notification = new NotificationRequest
        {
            NotificationId = DailyReminderNotificationId,
            Title = message.Title,
            Description = message.Body,
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = scheduledTime,
                RepeatType = NotificationRepeat.Daily
            }
        };

        await LocalNotificationCenter.Current.Show(notification);
    }

    public Task CancelDailyReminderAsync()
    {
        LocalNotificationCenter.Current.Cancel(DailyReminderNotificationId);
        return Task.CompletedTask;
    }

    public async Task ShowCoachNotificationAsync(string emoji, string title, string body)
    {
        var cleanTitle = string.IsNullOrWhiteSpace(emoji)
            ? title
            : $"{emoji} {title}";

        var notification = new NotificationRequest
        {
            NotificationId = CoachNotificationId,
            Title = cleanTitle,
            Description = body,
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = DateTime.Now.AddSeconds(2)
            }
        };

        await LocalNotificationCenter.Current.Show(notification);
    }

    public (string Title, string Body) GetReminderPreviewMessage()
    {
        var profile = _profileService.GetActiveProfile();
        return BuildReminderMessage(profile);
    }

    private (string Title, string Body) BuildReminderMessage(Models.ChildProfile? profile)
    {
        var daySeed = DateTime.Today.DayOfYear;
        var baseTitle = WarmTitles[daySeed % WarmTitles.Length];
        var name = profile?.Name?.Trim();

        if (profile is null)
            return ($"\u2728 {baseTitle}", PickByDay(StarterBodies));

        var dailyGoal = _profileService.GetDailyGoal(profile);
        var remaining = Math.Max(0, dailyGoal.TotalCount - dailyGoal.CompletedCount);
        var categoryVisual = GetFavoriteCategoryVisual(profile);
        var categoryHint = GetFavoriteCategoryHint(categoryVisual);
        var visualPrefix = string.IsNullOrWhiteSpace(categoryVisual.Emoji)
            ? string.Empty
            : $"{categoryVisual.Emoji} ";

        if (dailyGoal.CompletedCount >= dailyGoal.TotalCount)
            return ($"\U0001F31F {name}, harika gidiyorsun", $"{PickByDay(CompletedBodies)}{categoryHint}");

        if (profile.StreakDays >= 3)
            return ($"\U0001F525 {name}, serin devam etsin", $"{string.Format(PickByDay(StreakBodies), remaining)}{categoryHint}");

        if (remaining <= 1)
            return ($"\U0001F3AF {name}, neredeyse bitti", $"{PickByDay(NearlyDoneBodies)}{categoryHint}");

        return ($"\U0001F989 {baseTitle}", $"{visualPrefix}{name}, bugun {remaining} mini gorev seni bekliyor. {PickByDay(StarterBodies)}{categoryHint}");
    }

    private static string PickByDay(string[] values)
    {
        if (values.Length == 0)
            return string.Empty;

        return values[DateTime.Today.DayOfYear % values.Length];
    }

    private static string GetFavoriteCategoryHint((string Emoji, string Name) visual)
    {
        if (string.IsNullOrWhiteSpace(visual.Name))
            return string.Empty;

        var prefix = string.IsNullOrWhiteSpace(visual.Emoji)
            ? string.Empty
            : $"{visual.Emoji} ";
        return $" {prefix}En son keyif aldigin {visual.Name} tarafindan da devam edebiliriz.";
    }

    private static (string Emoji, string Name) GetFavoriteCategoryVisual(Models.ChildProfile profile)
    {
        var recentCategoryId = profile.CategoryProgresses.Values
            .OrderByDescending(progress => progress.LastPlayedAt)
            .Select(progress => progress.CategoryId)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(recentCategoryId))
            return (string.Empty, string.Empty);

        return recentCategoryId switch
        {
            "animals" => ("\U0001F43E", "hayvanlar"),
            "fruits" => ("\U0001F34E", "meyveler"),
            "vegetables" => ("\U0001F955", "sebzeler"),
            "colors" => ("\U0001F3A8", "renkler"),
            "shapes" => ("\U0001F539", "sekiller"),
            "vehicles" => ("\U0001F697", "araclar"),
            "numbers" => ("\U0001F522", "sayilar"),
            "letters" => ("\U0001F524", "harfler"),
            "emotions" => ("\U0001F60A", "duygular"),
            "planets" => ("\U0001FA90", "gezegenler"),
            "cities" => ("\U0001F3D9", "sehirler"),
            "countries" => ("\U0001F30D", "ulkeler"),
            "professions" => ("\U0001F4BC", "meslekler"),
            "nature" => ("\U0001F33F", "doga"),
            "objects" => ("\U0001F392", "nesneler"),
            "opposites" => ("\u2194", "zitlar"),
            "traffic" => ("\U0001F6A6", "trafik"),
            "weather" => ("\u2600", "hava durumu"),
            "seasons" => ("\U0001F338", "mevsimler"),
            _ => (string.Empty, string.Empty)
        };
    }
}

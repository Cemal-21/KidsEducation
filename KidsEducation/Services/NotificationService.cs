using Plugin.LocalNotification;

namespace KidsEducation.Services;

public class NotificationService
{
    private const int DailyReminderNotificationId = 1001;

    public async Task RequestPermissionAsync()
    {
        await LocalNotificationCenter.Current.RequestNotificationPermission();
    }

    public async Task ScheduleDailyReminderAsync(TimeSpan time)
    {
        await CancelDailyReminderAsync();

        var now = DateTime.Now;
        var scheduledTime = DateTime.Today.Add(time);
        if (scheduledTime <= now)
            scheduledTime = scheduledTime.AddDays(1);

        var notification = new NotificationRequest
        {
            NotificationId = DailyReminderNotificationId,
            Title = "Oynamayı unuttun! 🎮",
            Description = "Bugün henüz oynamadın. Hadi birkaç dakika eğlenceli bir oyun oynayalım!",
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = scheduledTime,
                RepeatType = NotificationRepeat.Daily
            }
        };

        await LocalNotificationCenter.Current.Show(notification);
    }

    public async Task CancelDailyReminderAsync()
    {
        LocalNotificationCenter.Current.Cancel(DailyReminderNotificationId);
        await Task.CompletedTask;
    }

    public async Task ShowCoachNotificationAsync(string emoji, string title, string body)
    {
        var notification = new NotificationRequest
        {
            NotificationId = 2001,
            Title = $"{emoji} {title}",
            Description = body,
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = DateTime.Now.AddSeconds(2)
            }
        };
        await LocalNotificationCenter.Current.Show(notification);
    }
}

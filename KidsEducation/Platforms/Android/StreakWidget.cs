using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;

namespace KidsEducation.Platforms.Android;

[BroadcastReceiver(Label = "Günlük Seri", Exported = true)]
[IntentFilter(new[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
[MetaData("android.appwidget.provider", Resource = "@xml/streak_widget_info")]
public class StreakWidget : AppWidgetProvider
{
    private const string PrefsName = "com.microsoft.maui.essentials.apppreferences";
    private const string StreakKey = "streak_days";
    private const string MotivationKey = "streak_motivation";

    public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
    {
        if (context is null || appWidgetManager is null || appWidgetIds is null) return;
        foreach (var id in appWidgetIds)
            UpdateWidget(context, appWidgetManager, id);
    }

    private static void UpdateWidget(Context context, AppWidgetManager manager, int widgetId)
    {
        var prefs = context.GetSharedPreferences(PrefsName, FileCreationMode.Private);
        var days = prefs?.GetInt(StreakKey, 0) ?? 0;
        var motivation = prefs?.GetString(MotivationKey, null) ?? GetMotivation(days);

        var views = new RemoteViews(context.PackageName!, Resource.Layout.streak_widget);
        views.SetTextViewText(Resource.Id.streak_count, days.ToString());
        views.SetTextViewText(Resource.Id.streak_motivation, motivation);

        var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
        if (launchIntent is not null)
        {
            var pendingIntent = PendingIntent.GetActivity(
                context, 0, launchIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
            views.SetOnClickPendingIntent(Resource.Id.streak_count, pendingIntent);
        }

        manager.UpdateAppWidget(widgetId, views);
    }

    private static string GetMotivation(int days) => days switch
    {
        0 => "Başla! 🚀",
        1 => "İlk gün! ⭐",
        < 7 => "Devam et! 💪",
        < 30 => "Harika! 🔥",
        _ => "Efsane! 🏆"
    };

    public static void NotifyUpdate(Context context)
    {
        var intent = new Intent(context, typeof(StreakWidget));
        intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
        var ids = AppWidgetManager.GetInstance(context)?
            .GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(StreakWidget))));
        if (ids is { Length: > 0 })
        {
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
            context.SendBroadcast(intent);
        }
    }
}

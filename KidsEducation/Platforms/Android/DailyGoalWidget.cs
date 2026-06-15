using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;

namespace KidsEducation.Platforms.Android;

[BroadcastReceiver(Label = "Günlük Hedef", Exported = true)]
[IntentFilter(new[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
[MetaData("android.appwidget.provider", Resource = "@xml/daily_goal_widget_info")]
public class DailyGoalWidget : AppWidgetProvider
{
    private const string PrefsName = "com.microsoft.maui.essentials.apppreferences";
    private const string DoneKey = "daily_goal_done";
    private const string TotalKey = "daily_goal_total";

    public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
    {
        if (context is null || appWidgetManager is null || appWidgetIds is null) return;
        foreach (var id in appWidgetIds)
            UpdateWidget(context, appWidgetManager, id);
    }

    private static void UpdateWidget(Context context, AppWidgetManager manager, int widgetId)
    {
        var prefs = context.GetSharedPreferences(PrefsName, FileCreationMode.Private);
        var done = prefs?.GetInt(DoneKey, 0) ?? 0;
        var total = prefs?.GetInt(TotalKey, 5) ?? 5;
        if (total <= 0) total = 5;
        var percent = Math.Min(100, done * 100 / total);

        var status = percent >= 100 ? "Tamamlandı! 🎉" : "Devam et! 💪";

        var views = new RemoteViews(context.PackageName!, Resource.Layout.daily_goal_widget);
        views.SetTextViewText(Resource.Id.goal_percent, $"%{percent}");
        views.SetProgressBar(Resource.Id.goal_progress, 100, percent, false);
        views.SetTextViewText(Resource.Id.goal_done_count, $"{done} / {total} aktivite");
        views.SetTextViewText(Resource.Id.goal_status, status);

        var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
        if (launchIntent is not null)
        {
            var pendingIntent = PendingIntent.GetActivity(
                context, 0, launchIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
            views.SetOnClickPendingIntent(Resource.Id.goal_percent, pendingIntent);
        }

        manager.UpdateAppWidget(widgetId, views);
    }

    public static void NotifyUpdate(Context context)
    {
        var intent = new Intent(context, typeof(DailyGoalWidget));
        intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
        var ids = AppWidgetManager.GetInstance(context)?
            .GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(DailyGoalWidget))));
        if (ids is { Length: > 0 })
        {
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
            context.SendBroadcast(intent);
        }
    }
}

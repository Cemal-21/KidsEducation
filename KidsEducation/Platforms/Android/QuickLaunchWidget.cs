using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;

namespace KidsEducation.Platforms.Android;

[BroadcastReceiver(Label = "Hızlı Başlat", Exported = true)]
[IntentFilter(new[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
[MetaData("android.appwidget.provider", Resource = "@xml/quick_launch_widget_info")]
public class QuickLaunchWidget : AppWidgetProvider
{
    public const string ActionQuiz     = "kidseducation.action.LAUNCH_QUIZ";
    public const string ActionMemory   = "kidseducation.action.LAUNCH_MEMORY";
    public const string ActionTopics   = "kidseducation.action.LAUNCH_TOPICS";
    public const string ActionGames    = "kidseducation.action.LAUNCH_GAMES";

    public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
    {
        if (context is null || appWidgetManager is null || appWidgetIds is null) return;
        foreach (var id in appWidgetIds)
            UpdateWidget(context, appWidgetManager, id);
    }

    private static void UpdateWidget(Context context, AppWidgetManager manager, int widgetId)
    {
        var views = new RemoteViews(context.PackageName!, Resource.Layout.quick_launch_widget);

        views.SetOnClickPendingIntent(Resource.Id.btn_quiz,   MakePendingIntent(context, ActionQuiz,   1));
        views.SetOnClickPendingIntent(Resource.Id.btn_memory, MakePendingIntent(context, ActionMemory, 2));
        views.SetOnClickPendingIntent(Resource.Id.btn_topics, MakePendingIntent(context, ActionTopics, 3));
        views.SetOnClickPendingIntent(Resource.Id.btn_games,  MakePendingIntent(context, ActionGames,  4));

        manager.UpdateAppWidget(widgetId, views);
    }

    private static PendingIntent MakePendingIntent(Context context, string action, int requestCode)
    {
        var intent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!)
                     ?? new Intent(Intent.ActionMain);
        intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
        intent.PutExtra("launch_action", action);
        return PendingIntent.GetActivity(
            context, requestCode, intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
    }

    public override void OnReceive(Context? context, Intent? intent)
    {
        base.OnReceive(context, intent);
        // Intent extras are read by MainActivity / App startup if needed
    }
}

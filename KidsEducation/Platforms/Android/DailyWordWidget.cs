using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using AndroidX.Core.Content;

namespace KidsEducation.Platforms.Android;

[BroadcastReceiver(Label = "Günün Kelimesi", Exported = true)]
[IntentFilter(new[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
[MetaData("android.appwidget.provider", Resource = "@xml/daily_word_widget_info")]
public class DailyWordWidget : AppWidgetProvider
{
    private const string PrefsName = "com.microsoft.maui.essentials.apppreferences";
    private const string NameTrKey = "daily_word_name_tr";
    private const string NameEnKey = "daily_word_name_en";
    private const string EmojiKey = "daily_word_emoji";
    private const string CategoryKey = "daily_word_category_name";
    private const string SentenceKey = "daily_word_sentence";

    public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
    {
        if (context is null || appWidgetManager is null || appWidgetIds is null) return;

        foreach (var widgetId in appWidgetIds)
            UpdateWidget(context, appWidgetManager, widgetId);
    }

    private static void UpdateWidget(Context context, AppWidgetManager manager, int widgetId)
    {
        var prefs = context.GetSharedPreferences(PrefsName, FileCreationMode.Private);

        var nameTr   = prefs?.GetString(NameTrKey, null) ?? "—";
        var nameEn   = prefs?.GetString(NameEnKey, null) ?? "";
        var emoji    = prefs?.GetString(EmojiKey, null) ?? "📖";
        var category = prefs?.GetString(CategoryKey, null) ?? "";
        var sentence = prefs?.GetString(SentenceKey, null) ?? "";

        var views = new RemoteViews(context.PackageName!, Resource.Layout.daily_word_widget);
        views.SetTextViewText(Resource.Id.widget_name_tr, nameTr);
        views.SetTextViewText(Resource.Id.widget_name_en, nameEn);
        views.SetTextViewText(Resource.Id.widget_emoji, emoji);
        views.SetTextViewText(Resource.Id.widget_category, category);
        views.SetTextViewText(Resource.Id.widget_sentence, sentence);

        // Uygulamayı açma niyeti
        var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
        if (launchIntent is not null)
        {
            var pendingIntent = PendingIntent.GetActivity(
                context, 0, launchIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
            views.SetOnClickPendingIntent(Resource.Id.widget_name_tr, pendingIntent);
        }

        manager.UpdateAppWidget(widgetId, views);
    }

    public static void NotifyUpdate(Context context)
    {
        var intent = new Intent(context, typeof(DailyWordWidget));
        intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
        var ids = AppWidgetManager.GetInstance(context)?
            .GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(DailyWordWidget))));
        if (ids is { Length: > 0 })
        {
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
            context.SendBroadcast(intent);
        }
    }
}

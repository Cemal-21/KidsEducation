namespace KidsEducation.Services;

public static class HapticService
{
    public static void Light()
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }

    public static void Success()
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            Task.Delay(80).ContinueWith(_ =>
            {
                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            });
        }
        catch { }
    }

    public static void Error()
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }
    }

    public static void Heavy()
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }
    }
}

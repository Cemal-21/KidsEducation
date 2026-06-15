namespace KidsEducation.Models;

public class ModuleProgressInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public int TotalCategories { get; set; }
    public int CompletedCategories { get; set; }
    public int PlayedCategories { get; set; }

    public double ProgressRatio => TotalCategories <= 0
        ? 0
        : Math.Min(1, CompletedCategories / (double)TotalCategories);

    public int ProgressPercent => (int)Math.Round(ProgressRatio * 100);
    public bool IsCompleted => TotalCategories > 0 && CompletedCategories >= TotalCategories;
    public string ProgressText => $"{CompletedCategories}/{TotalCategories}";
    public string StatusText => IsCompleted
        ? "Sertifika hazır"
        : PlayedCategories > 0
            ? $"{ProgressPercent}% tamamlandı"
            : "Başlanmadı";
    public string CertificateText => IsCompleted
        ? $"{Name} sertifikası kazanıldı"
        : $"{Math.Max(0, TotalCategories - CompletedCategories)} durak kaldı";
}

namespace KidsEducation.Models;

public class LearningItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string NameTr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string? SecondaryImagePath { get; set; }
    public string? AudioPath { get; set; }
    public string Category { get; set; } = string.Empty;
    public int DifficultyLevel { get; set; } = 1;

    // Kısa bilgi / kart altı bilgi
    public string? FunFact { get; set; }

    // Detay ekranında yazılı anlatım için
    public string? DescriptionTr { get; set; }

    // Sesli Tahmin oyunu için ElevenLabs ile üretilecek ipucu metni.
    public string? SoundClueText { get; set; }

    public bool HasSecondaryImage => !string.IsNullOrWhiteSpace(SecondaryImagePath);

    // Sesli anlatımda okunacak metin. JSON'da yoksa otomatik oluşturulur.
    public string SpeakText =>
        !string.IsNullOrWhiteSpace(DescriptionTr)
            ? $"{NameTr}. {DescriptionTr}"
            : $"{NameTr}. İngilizcesi {NameEn}. {FunFact}";
}

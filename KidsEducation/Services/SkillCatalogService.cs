using KidsEducation.Enums;
using KidsEducation.Models;

namespace KidsEducation.Services;

public class SkillCatalogService
{
    public const string CurriculumLabel = "MEB uyumlu gelişim alanı";

    private static readonly List<DevelopmentSkill> Skills = new()
    {
        new()
        {
            Id = "attention",
            Title = "Dikkat ve odaklanma",
            Area = "Bilişsel gelişim",
            Emoji = "🎯",
            Description = "Görseli takip etme, hedefe odaklanma ve yönergeyi sürdürme.",
            Suggestion = "Kısa süreli, tekrar eden mini oyunlarla dikkati güçlendirin."
        },
        new()
        {
            Id = "visual_discrimination",
            Title = "Görsel ayırt etme",
            Area = "Bilişsel gelişim",
            Emoji = "👀",
            Description = "Renk, şekil, parça-bütün ve benzerlik farklarını yakalama.",
            Suggestion = "Evde benzer iki nesnenin farklarını birlikte konuşun."
        },
        new()
        {
            Id = "vocabulary",
            Title = "Kelime hazinesi",
            Area = "Dil gelişimi",
            Emoji = "💬",
            Description = "Nesne adlarını tanıma, anlama ve tekrar etme.",
            Suggestion = "Gün içinde öğrenilen kelimeyi cümle içinde kullanın."
        },
        new()
        {
            Id = "listening",
            Title = "Dinleme ve anlama",
            Area = "Dil gelişimi",
            Emoji = "🔊",
            Description = "Sesli yönergeyi dinleme ve uygun görseli seçme.",
            Suggestion = "Kısa açıklamalar dinletip 'hangisiydi?' diye sorun."
        },
        new()
        {
            Id = "memory",
            Title = "Hafıza",
            Area = "Bilişsel gelişim",
            Emoji = "🧠",
            Description = "Görsel konumunu hatırlama ve eşleri bulma.",
            Suggestion = "Az kartla başlayıp başarı arttıkça kart sayısını yükseltin."
        },
        new()
        {
            Id = "matching",
            Title = "Eşleştirme",
            Area = "Bilişsel gelişim",
            Emoji = "🧩",
            Description = "Görsel, ad ve özellikleri doğru ilişkilendirme.",
            Suggestion = "Gerçek nesnelerle 'aynısını bul' oyunları oynayın."
        },
        new()
        {
            Id = "problem_solving",
            Title = "Problem çözme",
            Area = "Bilişsel gelişim",
            Emoji = "💡",
            Description = "İpucundan sonuca gitme ve seçenekleri eleme.",
            Suggestion = "Yanlış cevapta 'başka hangi ipucu var?' diye birlikte düşünün."
        },
        new()
        {
            Id = "early_math",
            Title = "Erken matematik",
            Area = "Bilişsel gelişim",
            Emoji = "🔢",
            Description = "Sayı, miktar, şekil ve sıralama farkındalığı.",
            Suggestion = "Oyuncakları sayma, gruplama ve karşılaştırma etkinlikleri yapın."
        },
        new()
        {
            Id = "classification",
            Title = "Sınıflandırma",
            Area = "Bilişsel gelişim",
            Emoji = "🗂️",
            Description = "Renk, tür, şekil veya kullanım amacına göre gruplama.",
            Suggestion = "Nesneleri renklerine veya türlerine göre sepetlere ayırın."
        },
        new()
        {
            Id = "self_regulation",
            Title = "Yönerge takibi",
            Area = "Sosyal-duygusal gelişim",
            Emoji = "✅",
            Description = "Sırayı bekleme, doğru anda dokunma ve oyunu tamamlama.",
            Suggestion = "Kısa hedefler koyup tamamladığında birlikte kutlayın."
        }
    };

    public IReadOnlyList<DevelopmentSkill> GetAllSkills() => Skills;

    public DevelopmentSkill? GetSkill(string skillId) =>
        Skills.FirstOrDefault(s => s.Id == skillId);

    public IReadOnlyList<string> GetSkillIdsForSession(GameSession session)
    {
        var ids = new List<string>();

        ids.AddRange(session.GameType switch
        {
            GameType.MatchName => new[] { "visual_discrimination", "vocabulary", "matching", "attention" },
            GameType.ShadowGuess => new[] { "visual_discrimination", "attention", "problem_solving" },
            GameType.ZoomGuess => new[] { "visual_discrimination", "attention", "self_regulation" },
            GameType.MemoryMatch => new[] { "memory", "matching", "visual_discrimination" },
            GameType.SoundGuess => new[] { "listening", "vocabulary", "attention" },
            GameType.BalloonPop => new[] { "attention", "visual_discrimination", "self_regulation" },
            _ => new[] { "attention" }
        });

        ids.AddRange(session.CategoryId switch
        {
            "numbers" => new[] { "early_math" },
            "shapes" => new[] { "early_math", "classification" },
            "colors" => new[] { "visual_discrimination", "classification" },
            "letters" => new[] { "vocabulary" },
            "english" => new[] { "vocabulary", "listening" },
            "animals" or "fruits" or "vegetables" or "vehicles" or "body" => new[] { "vocabulary", "classification" },
            _ => Array.Empty<string>()
        });

        return ids
            .Where(id => GetSkill(id) is not null)
            .Distinct()
            .ToList();
    }
}

public class DevelopmentSkill
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
}

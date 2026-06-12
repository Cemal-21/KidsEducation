using KidsEducation.Enums;
using KidsEducation.Models;

namespace KidsEducation.Services;

public class CurriculumActivityService
{
    private static readonly List<CurriculumActivity> Activities = new()
    {
        new()
        {
            Id = "listen_find_tell",
            SkillId = "listening",
            Title = "Dinle, bul, anlat",
            Emoji = "🔊",
            MebArea = "Türkçe alan becerileri · Dinleme/izleme ve konuşma",
            DurationText = "5 dk",
            Materials = "Uygulamadaki görseller veya evdeki 3 nesne",
            Goal = "Çocuğun kısa yönergeyi dinleyip doğru nesneyi seçmesini ve bir cümleyle anlatmasını destekler.",
            ParentPrompt = "Bunu neden seçtin? Bana bir özelliğini söyler misin?",
            StepsText = "1. Bir nesneyi kısa ipucuyla tarif edin.\n2. Çocuktan doğru nesneyi bulmasını isteyin.\n3. Bulduğu nesneyi rengi, sesi veya kullanımıyla anlatmasını isteyin."
        },
        new()
        {
            Id = "same_different_hunt",
            SkillId = "visual_discrimination",
            Title = "Aynı mı, farklı mı?",
            Emoji = "👀",
            MebArea = "Fen alan becerileri · Gözlem yapma ve sınıflandırma",
            DurationText = "6 dk",
            Materials = "2-4 oyuncak, meyve veya kart",
            Goal = "Benzerlik-farklılık farkındalığını ve dikkatli gözlem becerisini destekler.",
            ParentPrompt = "İkisinin hangi özelliği aynı, hangisi farklı?",
            StepsText = "1. İki nesneyi yan yana koyun.\n2. Çocuktan aynı olan bir özelliği bulmasını isteyin.\n3. Sonra farklı olan bir özelliği söylemesini isteyin."
        },
        new()
        {
            Id = "count_group_compare",
            SkillId = "early_math",
            Title = "Say, grupla, karşılaştır",
            Emoji = "🔢",
            MebArea = "Matematik alan becerileri · Sayma ve matematiksel muhakeme",
            DurationText = "7 dk",
            Materials = "Düğme, lego, kaşık veya oyuncaklar",
            Goal = "Sayma, az-çok karşılaştırma ve gruplama becerilerini destekler.",
            ParentPrompt = "Hangi grup daha çok? Nasıl anladın?",
            StepsText = "1. Nesneleri iki gruba ayırın.\n2. Her grubu birlikte sayın.\n3. Hangi grubun daha çok/az olduğunu konuşturun."
        },
        new()
        {
            Id = "memory_tray",
            SkillId = "memory",
            Title = "Tepside ne eksik?",
            Emoji = "🧠",
            MebArea = "Kavramsal beceriler · Hatırlama ve dikkat",
            DurationText = "5 dk",
            Materials = "Tepsi ve 4 küçük nesne",
            Goal = "Görsel hafıza, dikkat ve nesne adlandırmayı destekler.",
            ParentPrompt = "Sence hangisi kayboldu? Nerede duruyordu?",
            StepsText = "1. Tepsiye 4 nesne koyup 20 saniye inceletin.\n2. Çocuk gözünü kapatınca bir nesneyi kaldırın.\n3. Eksik nesneyi ve yerini söylemesini isteyin."
        },
        new()
        {
            Id = "move_with_instruction",
            SkillId = "self_regulation",
            Title = "Yönergeyle hareket et",
            Emoji = "✅",
            MebArea = "Hareket ve sağlık alan becerileri · Sosyal/bilişsel hareket becerileri",
            DurationText = "5 dk",
            Materials = "Boş alan",
            Goal = "Yönerge takibi, bekleme ve bedensel kontrolü destekler.",
            ParentPrompt = "Şimdi hangi yönergeyi hatırlıyorsun?",
            StepsText = "1. Tek adımlı yönerge verin: zıpla, dur, dön.\n2. Sonra iki adımlı yönergeye geçin: zıpla ve alkışla.\n3. Çocuk yönergeyi tamamlayınca sırayı ona verin."
        },
        new()
        {
            Id = "word_basket",
            SkillId = "vocabulary",
            Title = "Kelime sepeti",
            Emoji = "💬",
            MebArea = "Türkçe alan becerileri · Konuşma ve erken okuryazarlık",
            DurationText = "6 dk",
            Materials = "Evdeki nesneler",
            Goal = "Kelime hazinesi, kategori farkındalığı ve cümle kurmayı destekler.",
            ParentPrompt = "Bu nesneyle bir cümle kurabilir misin?",
            StepsText = "1. Bir kategori seçin: yiyecekler, taşıtlar, hayvanlar.\n2. Çocuktan o kategoriye uygun 3 nesne bulmasını isteyin.\n3. Her nesne için kısa bir cümle kurdurun."
        },
        new()
        {
            Id = "sort_like_a_scientist",
            SkillId = "classification",
            Title = "Bilim insanı gibi ayır",
            Emoji = "🗂️",
            MebArea = "Fen alan becerileri · Sınıflandırma",
            DurationText = "7 dk",
            Materials = "Karışık oyuncak veya mutfak nesneleri",
            Goal = "Nesneleri özelliklerine göre gruplama ve nedenini açıklama becerisini destekler.",
            ParentPrompt = "Bu grubu hangi kurala göre yaptın?",
            StepsText = "1. 6-8 nesneyi karışık koyun.\n2. Çocuktan kendi kuralına göre gruplamasını isteyin.\n3. Kuralını anlatmasını isteyin: renk, boyut, tür veya kullanım."
        },
        new()
        {
            Id = "clue_detective",
            SkillId = "problem_solving",
            Title = "İpucu dedektifi",
            Emoji = "💡",
            MebArea = "Kavramsal beceriler · Karşılaştırma, çıkarım ve problem çözme",
            DurationText = "6 dk",
            Materials = "3 görsel veya oyuncak",
            Goal = "İpucundan sonuca ulaşma, seçenek eleme ve gerekçe söyleme becerilerini destekler.",
            ParentPrompt = "Hangi ipucu seni cevaba götürdü?",
            StepsText = "1. Üç nesne koyun.\n2. Birini doğrudan söylemeden iki ipucuyla tarif edin.\n3. Çocuk cevabı bulunca hangi ipucunu kullandığını sorun."
        },
        new()
        {
            Id = "rhythm_repeat",
            SkillId = "attention",
            Title = "Ritmi dinle, tekrar et",
            Emoji = "🎵",
            MebArea = "Müzik alan becerileri · Müziksel dinleme ve hareket",
            DurationText = "4 dk",
            Materials = "El çırpma veya masa ritmi",
            Goal = "İşitsel dikkat, sıra bekleme ve ritim takibini destekler.",
            ParentPrompt = "Benim ritmim nasıldı, hızlı mı yavaş mı?",
            StepsText = "1. Kısa bir alkış ritmi yapın.\n2. Çocuktan aynısını tekrar etmesini isteyin.\n3. Sonra çocuğun ritmini siz tekrar edin."
        }
    };

    public IReadOnlyList<CurriculumActivity> GetRecommendedActivities(
        ChildProfile profile,
        IEnumerable<string> prioritySkillIds,
        int take = 3)
    {
        var preferredSkillIds = prioritySkillIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        if (preferredSkillIds.Count == 0)
            preferredSkillIds = GetDefaultSkillIds(profile.AgeGroup);

        var preferred = preferredSkillIds
            .SelectMany(skillId => Activities.Where(a => a.SkillId == skillId))
            .DistinctBy(a => a.Id)
            .ToList();

        return preferred
            .Concat(Activities)
            .DistinctBy(a => a.Id)
            .Take(take)
            .ToList();
    }

    private static List<string> GetDefaultSkillIds(AgeGroup ageGroup) => ageGroup switch
    {
        AgeGroup.Toddler => new() { "listening", "visual_discrimination", "self_regulation" },
        AgeGroup.Explorer => new() { "vocabulary", "early_math", "classification" },
        AgeGroup.Adventurer => new() { "problem_solving", "early_math", "memory" },
        _ => new() { "listening", "vocabulary", "attention" }
    };
}

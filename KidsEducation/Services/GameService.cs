using KidsEducation.Enums;
using KidsEducation.Models;

namespace KidsEducation.Services;

public class GameService
{
    private readonly LearningEventService _eventService;

    public GameService(LearningEventService eventService)
    {
        _eventService = eventService;
    }

    // Yeni oyun oturumu oluştur
    public GameSession CreateSession(
        string categoryId,
        GameType gameType,
        List<LearningItem> items,
        int optionCount = 4,
        int difficultyLevel = 1)
    {
        var session = new GameSession
        {
            CategoryId = categoryId,
            GameType = gameType,
            StartedAt = DateTime.UtcNow,
            DifficultyLevel = difficultyLevel
        };

        foreach (var correctItem in items)
        {
            var distractors = items
                .Where(i => i.Id != correctItem.Id)
                .OrderBy(_ => Guid.NewGuid())
                .Take(optionCount - 1)
                .ToList();

            var options = distractors
                .Append(correctItem)
                .OrderBy(_ => Guid.NewGuid())
                .ToList();

            session.Rounds.Add(new GameRound
            {
                CorrectItem = correctItem,
                Options = options
            });
        }

        return session;
    }

    // Cevabı kontrol et
    public bool SubmitAnswer(GameSession session, int roundIndex, string selectedItemId, string profileId)
    {
        if (roundIndex < 0 || roundIndex >= session.Rounds.Count)
            return false;

        var round = session.Rounds[roundIndex];
        round.SelectedItemId = selectedItemId;

        var isCorrect = selectedItemId == round.CorrectItem.Id;
        round.Result = isCorrect ? GameResult.Correct : GameResult.Wrong;

        _ = _eventService.LogEventAsync(new LearningEvent
        {
            ProfileId = profileId,
            CategoryId = session.CategoryId,
            ItemId = round.CorrectItem.Id,
            GameType = session.GameType.ToString(),
            IsCorrect = isCorrect,
            DifficultyLevel = session.DifficultyLevel
        });

        return isCorrect;
    }

    // Oturumu bitir
    public GameSession FinishSession(GameSession session)
    {
        session.FinishedAt = DateTime.UtcNow;
        return session;
    }

    // ── HAFIZA OYUNU ──────────────────────────────────────────

    /// <summary>
    /// Hafıza kartı oturumu oluşturur. Her item için iki özdeş kart (bir çift) üretilir
    /// ve kartlar karıştırılır.
    /// </summary>
    public MemorySession CreateMemorySession(string categoryId, List<LearningItem> items, int pairCount = 6, int difficultyLevel = 1)
    {
        var selected = items
            .OrderBy(_ => Guid.NewGuid())
            .Take(Math.Min(pairCount, items.Count))
            .ToList();

        var cards = new List<MemoryCard>();

        foreach (var item in selected)
        {
            for (int i = 0; i < 2; i++)
            {
                cards.Add(new MemoryCard
                {
                    ItemId = item.Id,
                    ImagePath = item.ImagePath,
                    NameTr = item.NameTr
                });
            }
        }

        cards = cards.OrderBy(_ => Guid.NewGuid()).ToList();

        return new MemorySession
        {
            CategoryId = categoryId,
            DifficultyLevel = difficultyLevel,
            Cards = cards
        };
    }

    /// <summary>
    /// İki kart seçildiğinde çağrılır. Eşleşirse ikisini de IsMatched=true yapar.
    /// </summary>
    public bool CheckMemoryMatch(MemorySession session, string firstCardId, string secondCardId)
    {
        var first = session.Cards.FirstOrDefault(c => c.Id == firstCardId);
        var second = session.Cards.FirstOrDefault(c => c.Id == secondCardId);

        if (first is null || second is null) return false;

        session.Moves++;

        var isMatch = first.ItemId == second.ItemId;

        if (isMatch)
        {
            first.IsMatched = true;
            second.IsMatched = true;
        }

        return isMatch;
    }

    public MemorySession FinishMemorySession(MemorySession session)
    {
        session.FinishedAt = DateTime.UtcNow;
        return session;
    }

    /// <summary>
    /// MemorySession'ı, mevcut ResultPage / UpdateProgress akışıyla uyumlu
    /// bir GameSession'a dönüştürür.
    /// </summary>
    public GameSession ToGameSession(MemorySession memorySession)
    {
        var gameSession = new GameSession
        {
            CategoryId = memorySession.CategoryId,
            GameType = GameType.MemoryMatch,
            StartedAt = memorySession.StartedAt,
            FinishedAt = memorySession.FinishedAt,
            DifficultyLevel = memorySession.DifficultyLevel
        };

        var matchedItemIds = memorySession.Cards
            .Where(c => c.IsMatched)
            .Select(c => c.ItemId)
            .Distinct()
            .ToList();

        for (int i = 0; i < memorySession.TotalPairs; i++)
        {
            var isMatched = i < memorySession.MatchedPairs;
            var itemId = i < matchedItemIds.Count ? matchedItemIds[i] : string.Empty;

            gameSession.Rounds.Add(new GameRound
            {
                CorrectItem = new LearningItem { Id = itemId },
                SelectedItemId = isMatched ? itemId : null,
                Result = isMatched ? GameResult.Correct : GameResult.Wrong
            });
        }

        return gameSession;
    }
}

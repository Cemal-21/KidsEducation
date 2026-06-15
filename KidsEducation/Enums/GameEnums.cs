namespace KidsEducation.Enums;

public enum AgeGroup
{
    Toddler = 1,      // 3-5 yaş — Minikler
    Explorer = 2,     // 5-7 yaş — Keşifçiler
    Adventurer = 3    // 7-9 yaş — Kaşifler
}

public enum GameType
{
    MatchName,     // Görsel göster, ismi seç
    ShadowGuess,   // Gölgeden tahmin
    ZoomGuess,     // Kısmi görüntüden tahmin
    MemoryMatch,   // Hafıza kartları
    SoundGuess,    // Sesi dinle, doğru görseli seç
    BalloonPop,    // Hedef görseli hızlıca patlat
    SequenceOrder, // Sıralı düşünme ve örüntü
    StoryQuiz,     // Kısa hikayeden doğru görseli seç
    Tracing,       // Harf/sayı izleme (parmakla çizim)
    PuzzleSwap,    // Görsel puzzle (parça takası)
    LetterDrop,    // Harf yerleştirme
    MathQuiz,      // Matematik
    WordScramble,  // Kelime bul / anagram
    Matching,      // Görsel-isim eşleştirme
    FindAndMark,   // Hangisi bu? (6 görsel)
    Coloring,      // Boyama oyunu
    Sorting        // Sıralama oyunu
}

public enum GameResult
{
    NotPlayed,
    Correct,
    Wrong
}

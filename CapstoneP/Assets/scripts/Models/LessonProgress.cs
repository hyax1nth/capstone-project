using System;

[Serializable]
public class LessonProgress
{
    public string status;
    public int stars;
    public int score;
    public long lastPlayedAt;

    public LessonProgress()
    {
        status = "locked";
        stars = 0;
        score = 0;
        lastPlayedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public static LessonProgress CreateUnlocked()
    {
        return new LessonProgress { status = "unlocked" };
    }
}

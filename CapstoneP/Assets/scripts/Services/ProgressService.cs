using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

/// <summary>
/// Handles progress writes, star calculation, stats updates and unlock logic.
/// </summary>
public class ProgressService : MonoBehaviour
{
    private DatabaseReference Root => FirebaseDatabase.DefaultInstance.RootReference;

    // Subjects and lesson counts can be configured here.
    public readonly string[] Subjects = { "Alphabet", "Numbers", "Reading", "Match" };
    public readonly int LessonsPerSubject = 5; // L1..L5

    /// <summary>
    /// Calculates stars following the spec.
    /// </summary>
    public int CalculateStars(string lessonType, int correct, int incorrect)
    {
        if (lessonType == "tracing") return 1;
        int total = correct + incorrect;
        if (total == 0) return 0; // no answers
        if (incorrect == 0) return 3;
        if (incorrect < correct) return 2;
        if (incorrect >= correct && correct > 0) return 1;
        return 0;
    }

    /// <summary>
    /// Record an attempt and update user stats and unlock next lesson.
    /// </summary>
    public async Task WriteAttempt(string uid, string subject, string lessonId, string lessonType, int correct, int incorrect, long timeSpentMs)
    {
        if (string.IsNullOrEmpty(uid)) throw new ArgumentNullException(nameof(uid));

        var stars = CalculateStars(lessonType, correct, incorrect);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // progress path
        var progressRef = Root.Child($"progress/{uid}/{subject}/{lessonId}");

        // Read existing progress to increment attempts
        var snapshot = await progressRef.GetValueAsync();
        int attemptsMade = 0;
        if (snapshot.Exists && snapshot.HasChild("attemptsMade"))
        {
            attemptsMade = int.Parse(snapshot.Child("attemptsMade").Value.ToString());
        }
        attemptsMade++;

        var progressObj = new Dictionary<string, object>
        {
            { "unlocked", true },
            { "completionStatus", stars > 0 },
            { "score", stars },
            { "timeSpentMs", timeSpentMs },
            { "attemptsMade", attemptsMade },
            { "lastPlayedAt", now },
            { "correct", correct },
            { "incorrect", incorrect },
            { "lessonType", lessonType }
        };

        await progressRef.UpdateChildrenAsync(progressObj);

        // Update user stats
        await UpdateUserStatsAfterAttempt(uid, subject, lessonId, stars);

        // Unlock next lesson if this completion succeeded
        if (stars > 0)
        {
            await UnlockNextLesson(uid, subject, lessonId);
        }
    }

    private async Task UpdateUserStatsAfterAttempt(string uid, string subject, string lessonId, int stars)
    {
        var statsRef = Root.Child($"users/{uid}/stats");
        var snapshot = await statsRef.GetValueAsync();

        UserStats stats = new UserStats();
        if (snapshot.Exists)
        {
            try
            {
                stats = JsonUtility.FromJson<UserStats>(snapshot.GetRawJsonValue()) ?? new UserStats();
            }
            catch { stats = new UserStats(); }
        }

        // update totalStars by summing unique lesson star values, but for simplicity increment by stars (idempotency not implemented here)
        stats.totalStars += stars;

        string lessonFullKey = $"{subject}/{lessonId}";
        if (!stats.lessonPlayCounts.ContainsKey(lessonFullKey)) stats.lessonPlayCounts[lessonFullKey] = 0;
        stats.lessonPlayCounts[lessonFullKey]++;
        stats.mostPlayedLesson = GetMostPlayedLesson(stats.lessonPlayCounts);

        if (stars > 0 && !stats.completedLessons.Contains(lessonFullKey))
        {
            stats.completedLessons.Add(lessonFullKey);
        }

        await statsRef.SetRawJsonValueAsync(JsonUtility.ToJson(stats));
    }

    private string GetMostPlayedLesson(Dictionary<string, int> counts)
    {
        string best = null;
        int bestCount = -1;
        foreach (var kv in counts)
        {
            if (kv.Value > bestCount)
            {
                best = kv.Key;
                bestCount = kv.Value;
            }
        }
        return best;
    }

    private async Task UnlockNextLesson(string uid, string subject, string lessonId)
    {
        // lessonId expected format "L1", "L2", ...
        if (!lessonId.StartsWith("L")) return;
        if (!int.TryParse(lessonId.Substring(1), out int num)) return;

        int next = num + 1;
        if (next > LessonsPerSubject) return;

        string nextLessonId = "L" + next;
        var nextRef = Root.Child($"progress/{uid}/{subject}/{nextLessonId}/unlocked");
        await nextRef.SetValueAsync(true);
    }
}

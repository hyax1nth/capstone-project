using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ProgressService
{
    private static ProgressService _instance;
    public static ProgressService Instance => _instance ??= new ProgressService();

    private readonly RealtimeDBService _db;
    private readonly string _progressPath;

    private ProgressService()
    {
        _db = RealtimeDBService.Instance;
        _progressPath = Constants.DatabasePaths.Progress;
    }

    public async Task InitializeSubjectIfNeeded(string userId, string subjectId)
    {
        try
        {
            var progressPath = $"{_progressPath}/{userId}/{subjectId}";
            var progress = await _db.GetAsync<Dictionary<string, LessonProgress>>(progressPath);

            if (progress == null || !progress.Any())
            {
                var initialProgress = new Dictionary<string, LessonProgress>();
                
                // Initialize all lessons as locked except first one
                for (int i = 0; i < Constants.LessonsPerSubject; i++)
                {
                    var lessonProgress = i == 0 ? 
                        LessonProgress.CreateUnlocked() : 
                        new LessonProgress();
                    
                    initialProgress[i.ToString()] = lessonProgress;
                }

                await _db.SetAsync(progressPath, initialProgress);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error initializing subject progress: {ex.Message}");
            throw;
        }
    }

    public async Task<Dictionary<string, LessonProgress>> GetSubjectProgress(string userId, string subjectId)
    {
        try
        {
            return await _db.GetAsync<Dictionary<string, LessonProgress>>($"{_progressPath}/{userId}/{subjectId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error getting subject progress: {ex.Message}");
            throw;
        }
    }

    public async Task CompleteLessonAsync(string userId, string subjectId, int lessonIndex, int stars, int score)
    {
        try
        {
            var progressPath = $"{_progressPath}/{userId}/{subjectId}";
            var subjectProgress = await GetSubjectProgress(userId, subjectId);
            
            if (subjectProgress == null)
            {
                throw new Exception("Subject progress not initialized");
            }

            // Update current lesson
            var currentProgress = subjectProgress.GetValueOrDefault(lessonIndex.ToString(), new LessonProgress());
            currentProgress.status = "completed";
            currentProgress.lastPlayedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            // Only update stars and score if better than previous
            if (stars > currentProgress.stars)
            {
                currentProgress.stars = stars;
                currentProgress.score = score;
            }

            subjectProgress[lessonIndex.ToString()] = currentProgress;

            // Unlock next lesson if available
            if (lessonIndex + 1 < Constants.LessonsPerSubject)
            {
                var nextProgress = subjectProgress.GetValueOrDefault((lessonIndex + 1).ToString(), new LessonProgress());
                nextProgress.status = "unlocked";
                subjectProgress[(lessonIndex + 1).ToString()] = nextProgress;
            }

            await _db.SetAsync(progressPath, subjectProgress);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error completing lesson: {ex.Message}");
            throw;
        }
    }

    public async Task<ProgressSummary> GetAggregateSummary(string userId, Dictionary<string, int> subjectLessonCounts)
    {
        try
        {
            var summary = new ProgressSummary();
            var topSubjects = new List<TopSubject>();

            foreach (var subject in subjectLessonCounts)
            {
                var progress = await GetSubjectProgress(userId, subject.Key);
                if (progress == null) continue;

                var completedLessons = progress.Count(p => p.Value.status == "completed");
                var totalStars = progress.Sum(p => p.Value.stars);
                var maxPossibleStars = completedLessons * 3;

                summary.totalLessons += subject.Value;
                summary.completedLessons += completedLessons;
                summary.starsCollected += totalStars;
                summary.starsMissing += maxPossibleStars - totalStars;

                if (completedLessons > 0)
                {
                    topSubjects.Add(new TopSubject
                    {
                        subjectId = subject.Key,
                        completionRate = (float)completedLessons / subject.Value,
                        avgStars = (float)totalStars / completedLessons
                    });
                }
            }

            // Sort and take top subjects by completion rate
            summary.topSubjects = topSubjects
                .OrderByDescending(s => s.completionRate)
                .ThenByDescending(s => s.avgStars)
                .ToList();

            return summary;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error getting aggregate summary: {ex.Message}");
            throw;
        }
    }
}

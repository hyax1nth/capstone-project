using System;
using System.Collections.Generic;

[Serializable]
public class UserStats
{
    public int totalStars;
    public List<string> completedLessons = new List<string>();
    public string mostPlayedLesson;
    public Dictionary<string, int> lessonPlayCounts = new Dictionary<string, int>();
}

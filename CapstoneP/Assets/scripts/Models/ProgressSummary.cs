using System;
using System.Collections.Generic;

[Serializable]
public class TopSubject
{
    public string subjectId;
    public float completionRate;
    public float avgStars;
}

[Serializable]
public class ProgressSummary
{
    public int totalLessons;
    public int completedLessons;
    public int starsCollected;
    public int starsMissing;
    public List<TopSubject> topSubjects;

    public ProgressSummary()
    {
        topSubjects = new List<TopSubject>();
    }
}

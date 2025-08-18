public static class LessonAPI
{
    public static void SetParams(string subject, string lessonId, string lessonType)
    {
        LessonParams.subject = subject;
        LessonParams.lessonId = lessonId;
        LessonParams.lessonType = lessonType;
    }
}

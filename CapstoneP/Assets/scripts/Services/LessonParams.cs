/// <summary>
/// Simple static container to pass lesson params between scenes without requiring a MonoBehaviour singleton.
/// Use before loading the lesson scene:
/// LessonParams.subject = "Alphabet"; LessonParams.lessonId = "L1"; LessonParams.lessonType = "template";
/// </summary>
public static class LessonParams
{
    public static string subject;
    public static string lessonId;
    public static string lessonType;
}

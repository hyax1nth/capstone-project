using UnityEngine;

/// <summary>
/// Lightweight in-memory container used to pass lesson parameters between dashboard and lesson scenes.
/// Persisted via DontDestroyOnLoad.
/// </summary>
public class LessonSession : MonoBehaviour
{
    public static LessonSession Instance { get; private set; }

    public string subject;
    public string lessonId;
    public string lessonType;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Set(string subject, string lessonId, string lessonType)
    {
        this.subject = subject;
        this.lessonId = lessonId;
        this.lessonType = lessonType;
    }
}

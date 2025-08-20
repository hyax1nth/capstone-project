using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small template for a lesson scene. Place this on a root manager GameObject in the lesson scene.
/// It reads `LessonSession.Instance` for subject/lessonId and invokes ProgressService when the fake lesson completes.
/// Replace with your real gameplay and call ReportResult when done.
/// </summary>
public class LessonTemplate : MonoBehaviour
{
    public Button finishButton;

    private ProgressService progressService;

    private void Start()
    {
    progressService = UnityEngine.Object.FindAnyObjectByType<ProgressService>();
        if (LessonSession.Instance == null)
        {
            UnityEngine.Debug.LogWarning("LessonTemplate: LessonSession not found. Make sure a LessonSession exists in the base scene.");
        }

        if (finishButton != null)
            finishButton.onClick.AddListener(OnFinishClicked);
    }

    public async void OnFinishClicked()
    {
        // Fake result for testing
    // Prefer LessonRegistry (set by StudentDashboard) then fallback to LessonSession
    string subject = LessonRegistry.Get("subject", LessonSession.Instance?.subject ?? "Alphabet");
    string lessonId = LessonRegistry.Get("lessonId", LessonSession.Instance?.lessonId ?? "L1");
    string lessonType = LessonRegistry.Get("lessonType", LessonSession.Instance?.lessonType ?? "template");
        int correct = 3;
        int incorrect = 0;
        long timeMs = 10000;

        var user = AuthManager.CurrentUser;
        if (user != null && progressService != null)
        {
            await progressService.WriteAttempt(user.UserId, subject, lessonId, lessonType, correct, incorrect, timeMs);
        }

        // Unload this lesson
        LessonLoader.Instance?.UnloadLesson(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}

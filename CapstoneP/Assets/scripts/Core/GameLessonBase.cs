using UnityEngine;
using System.Threading.Tasks;

public abstract class GameLessonBase : MonoBehaviour
{
    protected string SubjectId { get; private set; }
    protected int LessonIndex { get; private set; }
    protected const int MaxStars = 3;
    protected const float Duration = 30f;
    
    protected int StarsAwarded { get; set; }
    protected int Score { get; set; }

    protected bool IsGameActive { get; private set; }
    protected float RemainingTime { get; private set; }

    protected virtual void Start()
    {
        // Load lesson parameters from PlayerPrefs
        SubjectId = PlayerPrefs.GetString("CurrentSubjectId");
        LessonIndex = PlayerPrefs.GetInt("CurrentLessonIndex");

        if (string.IsNullOrEmpty(SubjectId))
        {
            Debug.LogError("No subject ID provided");
            SessionRouter.RouteToStudentDashboard();
            return;
        }

        InitializeLesson();
    }

    protected virtual void InitializeLesson()
    {
        StarsAwarded = 0;
        Score = 0;
        RemainingTime = Duration;
        IsGameActive = false;
    }

    public virtual void StartLesson()
    {
        IsGameActive = true;
    }

    protected virtual void Update()
    {
        if (!IsGameActive) return;

        RemainingTime -= Time.deltaTime;
        if (RemainingTime <= 0)
        {
            EndLesson(true);
        }
    }

    protected virtual void EndLesson(bool success)
    {
        IsGameActive = false;
        if (success)
        {
            ReportAndExit();
        }
        else
        {
            SessionRouter.RouteToStudentDashboard();
        }
    }

    protected async void ReportAndExit()
    {
        try
        {
            string userId = AuthManager.Instance.CurrentUserId;
            await ProgressService.Instance.CompleteLessonAsync(
                userId, 
                SubjectId, 
                LessonIndex, 
                StarsAwarded, 
                Score);

            SessionRouter.RouteToStudentDashboard();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error reporting lesson progress: {ex.Message}");
            SessionRouter.RouteToStudentDashboard();
        }
    }
}

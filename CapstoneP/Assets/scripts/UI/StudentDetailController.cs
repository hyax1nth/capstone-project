using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Threading.Tasks;

public class StudentDetailController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text NameText;
    public TMP_Text AgeText;
    public TMP_Text SubjectsText;
    public TMP_Text TotalLessonsText;
    public TMP_Text CompletedText;
    public TMP_Text StarsCollectedText;
    public TMP_Text StarsMissingText;
    public Transform TopSubjectsContainer;
    public GameObject TopSubjectItemPrefab;
    public Button DeleteButton;
    public Button BackButton;
    public TMP_Text Feedback;

    private string studentId;

    private void Start()
    {
        studentId = PlayerPrefs.GetString("ViewStudentId");
        if (string.IsNullOrEmpty(studentId))
        {
            Debug.LogError("No student ID provided");
            SessionRouter.RouteToAdminDashboard();
            return;
        }

        DeleteButton.onClick.AddListener(OnDeletePressed);
        BackButton.onClick.AddListener(() => SessionRouter.RouteToAdminDashboard());

        LoadStudentDetailsAsync();
    }

    private async void LoadStudentDetailsAsync()
    {
        try
        {
            var profile = await UserProfileService.Instance.GetProfile(studentId);
            if (profile == null)
            {
                Feedback.text = "Student not found";
                return;
            }

            // Display basic info
            NameText.text = profile.displayName;
            AgeText.text = $"Age: {profile.age}";
            SubjectsText.text = "Preferred Subjects: " + string.Join(", ", profile.preferredSubjects.Select(s => 
                char.ToUpper(s[0]) + s.Substring(1)));

            // Load progress summary
            var subjectCounts = await CatalogService.Instance.GetSubjectLessonCounts();
            var summary = await ProgressService.Instance.GetAggregateSummary(studentId, subjectCounts);

            // Display progress stats
            TotalLessonsText.text = $"Total Lessons: {summary.totalLessons}";
            CompletedText.text = $"Completed: {summary.completedLessons}";
            StarsCollectedText.text = $"Stars Collected: {summary.starsCollected}";
            StarsMissingText.text = $"Stars Missing: {summary.starsMissing}";

            // Display top subjects
            foreach (Transform child in TopSubjectsContainer)
                Destroy(child.gameObject);

            foreach (var subject in summary.topSubjects)
            {
                var item = Instantiate(TopSubjectItemPrefab, TopSubjectsContainer);
                var text = item.GetComponent<TMP_Text>();
                string subjectName = char.ToUpper(subject.subjectId[0]) + subject.subjectId.Substring(1);
                text.text = $"{subjectName}: {subject.completionRate:P0} complete, {subject.avgStars:F1} avg stars";
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading student details: {ex.Message}");
            Feedback.text = "Error loading student details";
        }
    }

    private async void OnDeletePressed()
    {
        try
        {
            DeleteButton.interactable = false;
            Feedback.text = "Deleting student data...";

            // Delete progress data
            await RealtimeDBService.Instance.DeleteAsync($"{Constants.DatabasePaths.Progress}/{studentId}");
            
            // Delete user profile
            await RealtimeDBService.Instance.DeleteAsync($"{Constants.DatabasePaths.Users}/{studentId}");

            SessionRouter.RouteToAdminDashboard();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error deleting student: {ex.Message}");
            Feedback.text = "Error deleting student";
            DeleteButton.interactable = true;
        }
    }

    private void OnDestroy()
    {
        if (DeleteButton != null)
            DeleteButton.onClick.RemoveListener(OnDeletePressed);
        if (BackButton != null)
            BackButton.onClick.RemoveListener(() => SessionRouter.RouteToAdminDashboard());
    }
}

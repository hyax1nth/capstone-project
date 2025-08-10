using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class StudentDashboardController : MonoBehaviour
{
    [Header("UI References")]
    public Transform SubjectsList;
    public GameObject SubjectRowPrefab;
    public GameObject LessonItemPrefab;
    public TMP_Text Feedback;

    private Dictionary<string, Transform> expandPanels = new Dictionary<string, Transform>();

    private void Start()
    {
        LoadSubjectsAsync();
    }

    private async void LoadSubjectsAsync()
    {
        try
        {
            string userId = AuthManager.Instance.CurrentUserId;
            var profile = await UserProfileService.Instance.GetProfile(userId);
            
            if (profile == null || profile.preferredSubjects == null)
            {
                Feedback.text = "Error loading subjects";
                return;
            }

            foreach (string subjectId in profile.preferredSubjects)
            {
                CreateSubjectRow(subjectId);
                await LoadLessonsForSubject(subjectId);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading subjects: {ex.Message}");
            Feedback.text = "Error loading subjects";
        }
    }

    private void CreateSubjectRow(string subjectId)
    {
        var row = Instantiate(SubjectRowPrefab, SubjectsList);
        row.name = $"Subject_{subjectId}";

        // Set subject title
        var title = row.GetComponentInChildren<TMP_Text>();
        title.text = char.ToUpper(subjectId[0]) + subjectId.Substring(1);

        // Set up expand/collapse
        var headerButton = row.GetComponentInChildren<Button>();
        var expandPanel = row.transform.Find("ExpandPanel");
        expandPanels[subjectId] = expandPanel;
        expandPanel.gameObject.SetActive(false);

        headerButton.onClick.AddListener(() => ToggleSubjectExpand(subjectId));
    }

    private void ToggleSubjectExpand(string subjectId)
    {
        if (expandPanels.TryGetValue(subjectId, out Transform panel))
        {
            bool isExpanded = panel.gameObject.activeSelf;
            panel.gameObject.SetActive(!isExpanded);
        }
    }

    private async Task LoadLessonsForSubject(string subjectId)
    {
        try
        {
            string userId = AuthManager.Instance.CurrentUserId;
            var progress = await ProgressService.Instance.GetSubjectProgress(userId, subjectId);

            if (progress == null) return;

            var expandPanel = expandPanels[subjectId];
            
            for (int i = 0; i < Constants.LessonsPerSubject; i++)
            {
                var lessonProgress = progress.GetValueOrDefault(i.ToString(), new LessonProgress());
                CreateLessonItem(expandPanel, subjectId, i, lessonProgress);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading lessons for {subjectId}: {ex.Message}");
        }
    }

    private void CreateLessonItem(Transform parent, string subjectId, int lessonIndex, LessonProgress progress)
    {
        var item = Instantiate(LessonItemPrefab, parent);
        item.name = $"Lesson_{lessonIndex}";

        var label = item.GetComponentInChildren<TMP_Text>();
        label.text = $"Level {lessonIndex + 1}";

        var status = item.transform.Find("Status").GetComponent<TMP_Text>();
        status.text = char.ToUpper(progress.status[0]) + progress.status.Substring(1);

        var stars = item.transform.Find("Stars").GetComponent<TMP_Text>();
        stars.text = new string('★', progress.stars) + new string('☆', 3 - progress.stars);

        var playButton = item.GetComponentInChildren<Button>();
        playButton.interactable = progress.status != "locked";
        playButton.onClick.AddListener(() => OnStartLesson(subjectId, lessonIndex));
    }

    private void OnStartLesson(string subjectId, int lessonIndex)
    {
        // Save current lesson info to PlayerPrefs for the lesson scene to access
        PlayerPrefs.SetString("CurrentSubjectId", subjectId);
        PlayerPrefs.SetInt("CurrentLessonIndex", lessonIndex);
        PlayerPrefs.Save();

        // Load appropriate lesson scene based on subject
        string sceneName = subjectId.ToLower() switch
        {
            "english" => Constants.Scenes.LessonSpeech,
            "math" => Constants.Scenes.LessonTap,
            "science" => Constants.Scenes.LessonDrag,
            "art" => Constants.Scenes.LessonSwipe,
            "music" => Constants.Scenes.LessonTap,
            _ => Constants.Scenes.LessonTap
        };

        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}

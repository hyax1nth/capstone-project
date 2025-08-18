using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Student dashboard controller. Wire UI elements in the inspector.
/// Subject buttons should call OpenSubject(subjectName).
/// </summary>
public class StudentDashboard : MonoBehaviour
{
    public Image avatarImage; // to display avatar; map avatar key -> sprite via avatarSprites
    public TMPro.TMP_Text nameText;
    public Button logoutButton;
    public Button exitButton;
    [Header("Home / Panels")]
    public GameObject homePanel; // panel that contains the 4 subject buttons (assign in inspector)

    [Serializable]
    public class SubjectPanel
    {
        public string subjectName;
        public Button subjectButton;
        public GameObject lessonsPanel; // panel with lesson buttons
        public List<Button> lessonButtons; // assume ordered L1..L5
    public Button panelBackButton; // assigned per-subject: returns to homePanel
    }

    // Expect exactly 4 subject panels: Alphabet, Numbers, Reading, Match
    // Expose each panel explicitly so you can assign them in the inspector easily.
    [Header("Subject Panels (assign exactly 4)")]
    public SubjectPanel alphabetPanel;
    public SubjectPanel numbersPanel;
    public SubjectPanel readingPanel;
    public SubjectPanel matchingPanel;

    // Internal list used at runtime (populated from inspector fields)
    private List<SubjectPanel> subjectPanels = new List<SubjectPanel>(4);

    public AuthManager authManager;
    public ProfileService profileService;
    public ProgressService progressService;

    private void Start()
    {
    logoutButton.onClick.AddListener(() => {
        try { auth_manager_signout(); } catch { }
    });
    exitButton.onClick.AddListener(() => {
        // ensure PlayerPrefs are flushed so session persistence is written
        PlayerPrefs.Save();
        Application.Quit();
    });

        // Build the runtime list from the explicit inspector fields
        subjectPanels.Clear();
        subjectPanels.Add(alphabetPanel);
        subjectPanels.Add(numbersPanel);
        subjectPanels.Add(readingPanel);
        subjectPanels.Add(matchingPanel);

        // Validate inspector wiring and wire callbacks
        for (int s = 0; s < subjectPanels.Count; s++)
        {
            var sp = subjectPanels[s];
            if (sp == null)
            {
                Debug.LogWarning($"StudentDashboard: subject panel at index {s} is not assigned in the inspector.");
                continue;
            }

            if (sp.subjectButton != null)
            {
                string subject = sp.subjectName;
                sp.subjectButton.onClick.AddListener(() => ToggleSubjectPanel(subject));
            }
            else
            {
                Debug.LogWarning($"StudentDashboard: subjectButton for '{sp.subjectName}' is not assigned.");
            }

            // Expect lessonButtons to contain 5 buttons (L1..L5). They must be assigned in the inspector.
            for (int i = 0; i < sp.lessonButtons.Count; i++)
            {
                int index = i;
                string subject = sp.subjectName; // capture for closure
                if (sp.lessonButtons[i] != null)
                    sp.lessonButtons[i].onClick.AddListener(() => OnLessonButtonClicked(subject, "L" + (index + 1)));
                else
                    Debug.LogWarning($"StudentDashboard: lesson button {i} for '{sp.subjectName}' is not assigned.");
            }

            // Wire the per-panel back button (returns to homePanel)
            if (sp.panelBackButton != null)
            {
                sp.panelBackButton.onClick.AddListener(() =>
                {
                    if (sp.lessonsPanel != null) sp.lessonsPanel.SetActive(false);
                    if (homePanel != null) homePanel.SetActive(true);
                });
            }
            else
            {
                Debug.LogWarning($"StudentDashboard: panelBackButton for '{sp.subjectName}' is not assigned.");
            }
        }

        // Load profile and set avatar/name
        StartCoroutine(LoadProfileAndPopulate());
    }

    private void auth_manager_signout()
    {
        if (authManager != null) authManager.SignOut();
        // clear persisted uid
        PlayerPrefs.DeleteKey("lastSignedInUid");
        PlayerPrefs.Save();
        // return to Login scene if available
        try { UnityEngine.SceneManagement.SceneManager.LoadScene("Login"); } catch { }
    }

    private System.Collections.IEnumerator LoadProfileAndPopulate()
    {
        // wait a short frame to ensure AuthManager is initialized
        yield return null;

        var user = AuthManager.CurrentUser;
        if (user == null) yield break;

        var task = profileService.LoadProfile(user.UserId);
        while (!task.IsCompleted) yield return null;
        if (task.Result.Exists)
        {
            var snap = task.Result;
            if (snap.HasChild("displayName")) nameText.text = snap.Child("displayName").Value.ToString();
            if (snap.HasChild("avatar"))
            {
                string avatarKey = snap.Child("avatar").Value.ToString();
                // avatarKey expected like "avatar_0"
                // Try to load a sprite from Resources using the avatar key, e.g. Resources/Avatars/avatar_0
                var res = Resources.Load<Sprite>("Avatars/" + avatarKey);
                if (res != null)
                {
                    avatarImage.sprite = res;
                }
            }
        }
    }

    public void ToggleSubjectPanel(string subject)
    {
        // Hide the home panel and show only the requested subject panel while hiding others
        if (homePanel != null) homePanel.SetActive(false);

        for (int i = 0; i < subjectPanels.Count; i++)
        {
            var p = subjectPanels[i];
            if (p == null) continue;
            bool isTarget = p.subjectName == subject;
            if (p.lessonsPanel != null) p.lessonsPanel.SetActive(isTarget);
            if (isTarget) RefreshLessonLocks(p);
        }
    }

    private async void RefreshLessonLocks(SubjectPanel sp)
    {
        var user = AuthManager.CurrentUser;
        if (user == null) return;

        for (int i = 0; i < sp.lessonButtons.Count; i++)
        {
            string lid = "L" + (i + 1);
            var snapshot = await Firebase.Database.FirebaseDatabase.DefaultInstance.RootReference
                .Child($"progress/{user.UserId}/{sp.subjectName}/{lid}").GetValueAsync();

            bool unlocked = false;
            if (snapshot.Exists && snapshot.Child("unlocked").Exists)
            {
                unlocked = bool.Parse(snapshot.Child("unlocked").Value.ToString());
            }

            sp.lessonButtons[i].interactable = unlocked;
        }
    }

    private void OnLessonButtonClicked(string subject, string lessonId)
    {
    Debug.Log($"Open lesson {subject}/{lessonId}");
    // Set lesson params for the lesson scene to read via LessonRegistry
    LessonRegistry.Set("subject", subject);
    LessonRegistry.Set("lessonId", lessonId);
    LessonRegistry.Set("lessonType", "template");

    // Use scene naming convention: Lesson_{subject}_{lessonId}, e.g. Lesson_Alphabet_L1
    string sceneName = $"Lesson_{subject}_{lessonId}";
    var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
    if (op == null) Debug.LogError($"Failed to load lesson scene '{sceneName}'. Make sure it's added to Build Settings.");
    }

    // Close all subject panels (public so you can assign to a Back button)
    public void CloseAllSubjectPanels()
    {
        foreach (var sp in subjectPanels)
        {
            if (sp.lessonsPanel != null) sp.lessonsPanel.SetActive(false);
        }
    }
}

// Local helper to set lesson params (keeps compilation simple)
public static class _StudentDashboard_LessonAPI
{
    public static void SetParams(string subject, string lessonId, string lessonType)
    {
        // Mirror LessonParams if available; otherwise store in this static holder
        try
        {
            LessonParams.subject = subject;
            LessonParams.lessonId = lessonId;
            LessonParams.lessonType = lessonType;
            return;
        }
        catch { }

        // fallback: no-op (the external LessonParams might not be compiled yet in this environment)
    }
}

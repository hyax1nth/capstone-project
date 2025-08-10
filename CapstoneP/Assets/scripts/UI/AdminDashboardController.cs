using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Database;

public class AdminDashboardController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField SearchBar;
    public Transform ListContainer;
    public GameObject StudentRowPrefab;
    public TMP_Text Feedback;
    public Button SignOutButton;

    private void Start()
    {
        SearchBar.onValueChanged.AddListener(OnSearchChanged);
        if (SignOutButton != null)
            SignOutButton.onClick.AddListener(OnSignOutPressed);
            
        // Initial load
        LoadStudents("");
    }

    private void OnSearchChanged(string searchText)
    {
        LoadStudents(searchText.ToLower());
    }

    private async void LoadStudents(string searchPrefix)
    {
        try
        {
            // Clear existing rows
            foreach (Transform child in ListContainer)
            {
                Destroy(child.gameObject);
            }

            Feedback.text = "Loading...";

            // Query users ordered by displayNameLower
            var query = RealtimeDBService.Instance
                .OrderByChild(Constants.DatabasePaths.Users, "displayNameLower");

            if (!string.IsNullOrEmpty(searchPrefix))
            {
                query = query.StartAt(searchPrefix).EndAt(searchPrefix + "\uf8ff");
            }

            var snapshot = await query.GetValueAsync();
            if (!snapshot.Exists)
            {
                Feedback.text = "No students found";
                return;
            }

            var students = new List<(string uid, UserProfile profile)>();
            foreach (var child in snapshot.Children)
            {
                try
                {
                    var profile = JsonUtility.FromJson<UserProfile>(child.GetRawJsonValue());
                    if (profile.role != "admin") // Only show students
                    {
                        students.Add((child.Key, profile));
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error parsing student data: {ex.Message}");
                }
            }

            if (students.Count == 0)
            {
                Feedback.text = "No students found";
                return;
            }

            // Create student rows
            foreach (var (uid, profile) in students)
            {
                CreateStudentRow(uid, profile);
            }

            Feedback.text = $"Found {students.Count} student(s)";
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading students: {ex.Message}");
            Feedback.text = "Error loading students";
        }
    }

    private void CreateStudentRow(string uid, UserProfile profile)
    {
        var row = Instantiate(StudentRowPrefab, ListContainer);
        row.name = $"Student_{uid}";

        var nameText = row.transform.Find("Name").GetComponent<TMP_Text>();
        nameText.text = profile.displayName;

        var viewButton = row.GetComponentInChildren<Button>();
        viewButton.onClick.AddListener(() => OnViewStudent(uid));
    }

    private void OnViewStudent(string uid)
    {
        // Save student ID for detail view
        PlayerPrefs.SetString("ViewStudentId", uid);
        PlayerPrefs.Save();

        UnityEngine.SceneManagement.SceneManager.LoadScene(Constants.Scenes.StudentDetail);
    }

    private async void OnSignOutPressed()
    {
        try
        {
            SignOutButton.interactable = false;
            await AuthManager.Instance.SignOutAsync();
            SessionRouter.RouteToMainMenu();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error signing out: {ex.Message}");
            SignOutButton.interactable = true;
        }
    }

    private void OnDestroy()
    {
        if (SearchBar != null)
            SearchBar.onValueChanged.RemoveListener(OnSearchChanged);
            
        if (SignOutButton != null)
            SignOutButton.onClick.RemoveListener(OnSignOutPressed);
    }
}

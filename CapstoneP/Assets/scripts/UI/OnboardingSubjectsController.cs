using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class OnboardingSubjectsController : MonoBehaviour
{
    [Header("UI References")]
    public ToggleGroup SubjectsGroup;
    public TMP_Text Feedback;
    public Button FinishButton;

    private Dictionary<string, Toggle> subjectToggles = new Dictionary<string, Toggle>();

    private void Start()
    {
        // Get all toggles in the group and map them to subject names
        var toggles = SubjectsGroup.GetComponentsInChildren<Toggle>();
        foreach (var toggle in toggles)
        {
            string subjectId = toggle.name.ToLower();
            subjectToggles[subjectId] = toggle;
            toggle.onValueChanged.AddListener((_) => ValidateSelection());
        }

        FinishButton.onClick.AddListener(OnFinishPressed);
        ValidateSelection();
    }

    private void ValidateSelection()
    {
        bool hasSelection = subjectToggles.Values.Any(t => t.isOn);
        FinishButton.interactable = hasSelection;
        
        if (!hasSelection)
        {
            Feedback.text = "Please select at least one subject";
        }
        else
        {
            Feedback.text = "";
        }
    }

    private async void OnFinishPressed()
    {
        try
        {
            FinishButton.interactable = false;
            Feedback.text = "Saving...";

            string userId = AuthManager.Instance.CurrentUserId;
            var profile = await UserProfileService.Instance.GetProfile(userId);
            
            if (profile == null)
            {
                profile = new UserProfile();
            }

            // Get selected subjects
            profile.preferredSubjects = subjectToggles
                .Where(kvp => kvp.Value.isOn)
                .Select(kvp => kvp.Key)
                .ToArray();

            // Save profile
            await UserProfileService.Instance.SaveProfile(userId, profile);

            // Initialize progress for selected subjects
            foreach (string subject in profile.preferredSubjects)
            {
                await ProgressService.Instance.InitializeSubjectIfNeeded(userId, subject);
            }

            SessionRouter.RouteToStudentDashboard();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error saving subjects: {ex.Message}");
            Feedback.text = "An error occurred. Please try again.";
            FinishButton.interactable = true;
        }
    }

    private void OnDestroy()
    {
        if (FinishButton != null)
            FinishButton.onClick.RemoveListener(OnFinishPressed);

        foreach (var toggle in subjectToggles.Values)
        {
            if (toggle != null)
                toggle.onValueChanged.RemoveAllListeners();
        }
    }
}

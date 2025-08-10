using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class OnboardingAgeController : MonoBehaviour
{
    [Header("UI References")]
    public Button[] AgeButtons; // Should have 7 buttons for ages 4-10
    public TMP_Text Feedback;
    public Button NextButton;

    private int selectedAge = -1;

    private void Start()
    {
        NextButton.interactable = false;
        
        // Set up age buttons
        for (int i = 0; i < AgeButtons.Length; i++)
        {
            int age = i + Constants.MinAge; // Maps 0-6 to ages 4-10
            AgeButtons[i].GetComponentInChildren<TMP_Text>().text = age.ToString();
            
            // Store the age value in a local variable for the closure
            int buttonAge = age;
            AgeButtons[i].onClick.AddListener(() => OnAgeSelected(buttonAge));
        }

        NextButton.onClick.AddListener(OnNextPressed);
    }

    private void OnAgeSelected(int age)
    {
        selectedAge = age;
        
        // Update visual feedback
        for (int i = 0; i < AgeButtons.Length; i++)
        {
            bool isSelected = (i + Constants.MinAge) == age;
            AgeButtons[i].GetComponent<Image>().color = isSelected ? Color.green : Color.white;
        }

        NextButton.interactable = true;
    }

    private async void OnNextPressed()
    {
        try
        {
            NextButton.interactable = false;
            Feedback.text = "Saving...";

            string userId = AuthManager.Instance.CurrentUserId;
            var profile = await UserProfileService.Instance.GetProfile(userId);
            
            if (profile == null)
            {
                profile = new UserProfile();
            }

            profile.age = selectedAge;
            await UserProfileService.Instance.SaveProfile(userId, profile);

            SessionRouter.RouteToOnboardingSubjects();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error saving age: {ex.Message}");
            Feedback.text = "An error occurred. Please try again.";
            NextButton.interactable = true;
        }
    }

    private void OnDestroy()
    {
        if (NextButton != null)
            NextButton.onClick.RemoveListener(OnNextPressed);

        if (AgeButtons != null)
        {
            foreach (var button in AgeButtons)
            {
                if (button != null)
                    button.onClick.RemoveAllListeners();
            }
        }
    }
}

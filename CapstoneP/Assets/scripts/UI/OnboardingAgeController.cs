using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class OnboardingAgeController : MonoBehaviour
{
    [Header("UI References")]
    public Button[] AgeButtons; // Should have 7 buttons for ages 1-7
    public TMP_Text Feedback;
    public Button NextButton;

    [Header("Button Style")]
    [SerializeField] private Color selectedColor = new Color(0.8f, 0.6f, 0f); // Darker yellow
    [SerializeField] private Color normalColor = Color.yellow;

    private int selectedAge = -1;
    private ButtonPressAnimation[] buttonAnimations;

    private void Start()
    {
        NextButton.interactable = false;
        buttonAnimations = new ButtonPressAnimation[AgeButtons.Length];
        
        // Set up age buttons
        for (int i = 0; i < AgeButtons.Length; i++)
        {
            int age = i + Constants.MinAge; // Maps 0-6 to ages 1-7
            AgeButtons[i].GetComponentInChildren<TMP_Text>().text = age.ToString();
            
            // Get or add ButtonPressAnimation component
            buttonAnimations[i] = AgeButtons[i].GetComponent<ButtonPressAnimation>();
            if (buttonAnimations[i] == null)
            {
                buttonAnimations[i] = AgeButtons[i].gameObject.AddComponent<ButtonPressAnimation>();
            }
            
            // Set initial color
            AgeButtons[i].GetComponent<Image>().color = normalColor;
            
            // Store the age value in a local variable for the closure
            int buttonAge = age;
            AgeButtons[i].onClick.AddListener(() => OnAgeSelected(buttonAge));
        }

        NextButton.onClick.AddListener(OnNextPressed);
    }

    private void OnAgeSelected(int age)
    {
        selectedAge = age;
        
        // Update visual feedback for all buttons
        for (int i = 0; i < AgeButtons.Length; i++)
        {
            bool isSelected = (i + Constants.MinAge) == age;
            var image = AgeButtons[i].GetComponent<Image>();
            image.color = isSelected ? selectedColor : normalColor;
            
            // Keep selected button in pressed state
            if (buttonAnimations[i] != null)
            {
                if (isSelected)
                {
                    buttonAnimations[i].SetStayPressed(true);
                }
                else
                {
                    buttonAnimations[i].SetStayPressed(false);
                }
            }
        }
        
        NextButton.interactable = true;
    }

    private async void OnNextPressed()
    {
        try
        {
            Debug.Log("Next button pressed - attempting to save age and navigate");
            Debug.Log($"Selected age: {selectedAge}");
            
            if (!AuthManager.Instance.IsSignedIn)
            {
                Debug.LogError("User is not signed in!");
                Feedback.text = "Please sign in again.";
                SessionRouter.RouteToSignIn();
                return;
            }

            NextButton.interactable = false;
            Feedback.text = "Saving...";

            string userId = AuthManager.Instance.CurrentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError("User ID is null or empty!");
                Feedback.text = "Authentication error. Please sign in again.";
                SessionRouter.RouteToSignIn();
                return;
            }
            Debug.Log($"Current user ID: {userId}");
            
            var profile = await UserProfileService.Instance.GetProfile(userId);
            Debug.Log($"Retrieved profile: {(profile != null ? "yes" : "no")}");
            
            if (profile == null)
            {
                profile = new UserProfile();
                Debug.Log("Created new profile");
            }

            profile.age = selectedAge;

            if (UserProfileService.Instance == null)
            {
                Debug.LogError("UserProfileService.Instance is null!");
                Feedback.text = "Service error. Please try again.";
                NextButton.interactable = true;
                return;
            }

            await UserProfileService.Instance.SaveProfile(userId, profile);
            Debug.Log("Profile saved successfully");

            Debug.Log("Attempting to navigate to OnboardingSubjects scene");
            UnityEngine.SceneManagement.SceneManager.LoadScene(Constants.Scenes.OnboardingSubjects);
            Debug.Log("Navigation command sent");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in OnNextPressed: {ex.Message}");
            Debug.LogError($"Stack trace: {ex.StackTrace}");
            Feedback.text = $"Error: {ex.Message}";
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

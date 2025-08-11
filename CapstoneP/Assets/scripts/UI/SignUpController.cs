using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using TMPro;

public class SignUpController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField DisplayName;
    public TMP_InputField Email;
    public TMP_InputField Password;
    public Toggle ShowPassword;
    public TMP_Text Feedback;
    public Button SignUpButton;
    public Button BackButton;

    private void Start()
    {
        ShowPassword.onValueChanged.AddListener(OnShowPasswordChanged);
        SignUpButton.onClick.AddListener(OnSignUpPressed);
        if (BackButton != null)
            BackButton.onClick.AddListener(() => SessionRouter.RouteToMainMenu());
    }

    private void OnShowPasswordChanged(bool show)
    {
        Password.contentType = show ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
        Password.ForceLabelUpdate();
    }

    public async void OnSignUpPressed()
    {
        if (string.IsNullOrEmpty(DisplayName.text.Trim()))
        {
            Feedback.text = "Please enter your display name.";
            return;
        }

        try
        {
            SignUpButton.interactable = false;
            Feedback.text = "Creating account...";

            var (success, message) = await AuthManager.Instance.SignUpAsync(Email.text, Password.text);
            if (!success)
            {
                Feedback.text = message;
                SignUpButton.interactable = true;
                return;
            }

            string userId = AuthManager.Instance.CurrentUserId;
            var profile = new UserProfile
            {
                displayName = DisplayName.text.Trim(),
                displayNameLower = DisplayName.text.Trim().ToLower(),
                role = "student"
            };

            await UserProfileService.Instance.SaveProfile(userId, profile);
            SessionRouter.RouteToOnboardingAge();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Sign up error: {ex.Message}");
            Feedback.text = "An error occurred. Please try again.";
            SignUpButton.interactable = true;
        }
    }

    private void OnDestroy()
    {
        if (ShowPassword != null)
            ShowPassword.onValueChanged.RemoveListener(OnShowPasswordChanged);
        
        if (SignUpButton != null)
            SignUpButton.onClick.RemoveListener(OnSignUpPressed);
    }
}

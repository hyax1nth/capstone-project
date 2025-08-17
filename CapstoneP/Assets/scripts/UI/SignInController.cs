using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using TMPro;

public class SignInController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField Email;
    public TMP_InputField Password;
    public Toggle ShowPassword;
    public TMP_Text Feedback;
    public Button SignInButton;

    private void Start()
    {
        ShowPassword.onValueChanged.AddListener(OnShowPasswordChanged);
        SignInButton.onClick.AddListener(OnSignInPressed);
    }

    private void OnShowPasswordChanged(bool show)
    {
        Password.contentType = show ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
        Password.ForceLabelUpdate();
    }

    public async void OnSignInPressed()
    {
        try
        {
            SignInButton.interactable = false;
            Feedback.text = "Signing in...";

            var (success, message) = await AuthManager.Instance.SignInAsync(Email.text, Password.text);
            if (!success)
            {
                Feedback.text = message;
                SignInButton.interactable = true;
                return;
            }

            string userId = AuthManager.Instance.CurrentUserId;
            string role = await UserProfileService.Instance.GetRole(userId);

            if (role == "admin")
            {
                SessionRouter.RouteToAdminDashboard();
                return;
            }

            bool isProfileComplete = await UserProfileService.Instance.EnsureProfileCompleteness(userId);
            if (!isProfileComplete)
            {
                SessionRouter.RouteToOnboardingAge();
                return;
            }

            SessionRouter.RouteToStudentDashboard();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Sign in error: {ex.Message}");
            Feedback.text = "An error occurred. Please try again.";
            SignInButton.interactable = true;
        }
    }

    private void OnDestroy()
    {
        if (ShowPassword != null)
            ShowPassword.onValueChanged.RemoveListener(OnShowPasswordChanged);
        
        if (SignInButton != null)
            SignInButton.onClick.RemoveListener(OnSignInPressed);
    }
}

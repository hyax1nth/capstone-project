using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using TMPro;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button signInButton;
    [SerializeField] private Button signUpButton;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private CanvasGroup mainButtonsGroup;

    private void Start()
    {
        // Ensure UI is in the correct initial state
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        if (mainButtonsGroup != null) mainButtonsGroup.interactable = true;
        
        // Add button listeners
        if (signInButton != null) signInButton.onClick.AddListener(OnSignInClick);
        if (signUpButton != null) signUpButton.onClick.AddListener(OnSignUpClick);

        // Animate title if needed
        AnimateTitleEntry();
    }

    private void OnDestroy()
    {
        // Clean up listeners
        if (signInButton != null) signInButton.onClick.RemoveListener(OnSignInClick);
        if (signUpButton != null) signUpButton.onClick.RemoveListener(OnSignUpClick);
    }

    private async void OnSignInClick()
    {
        await HandleButtonClick(() => SessionRouter.RouteToSignIn());
    }

    private async void OnSignUpClick()
    {
        await HandleButtonClick(() => SessionRouter.RouteToSignUp());
    }

    private async Task HandleButtonClick(System.Action navigationAction)
    {
        // Disable buttons during transition
        SetUIInteractable(false);
        
        try
        {
            // Add a small delay to show loading state
            await Task.Delay(100);
            navigationAction?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Navigation error: {e.Message}");
            // Re-enable UI if navigation fails
            SetUIInteractable(true);
        }
    }

    private void SetUIInteractable(bool interactable)
    {
        if (mainButtonsGroup != null)
        {
            mainButtonsGroup.interactable = interactable;
            mainButtonsGroup.alpha = interactable ? 1f : 0.5f;
        }
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(!interactable);
        }
    }

    private void AnimateTitleEntry()
    {
        if (titleText == null) return;

        // Simple scale animation using coroutine
        titleText.transform.localScale = Vector3.zero;
        StartCoroutine(ScaleInTitle());
    }

    private System.Collections.IEnumerator ScaleInTitle()
    {
        yield return new WaitForSeconds(0.5f); // Initial delay

        float duration = 1f;
        float startTime = Time.time;
        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = Vector3.one;

        while (Time.time < startTime + duration)
        {
            float progress = (Time.time - startTime) / duration;
            // Simple easing function
            progress = 1f - ((1f - progress) * (1f - progress));
            titleText.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            yield return null;
        }

        titleText.transform.localScale = targetScale;
    }
}

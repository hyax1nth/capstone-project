using UnityEngine;
using UnityEngine.UI;

public class BackToMenuButton : MonoBehaviour
{
    [SerializeField] private Button backButton;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Start()
    {
        // Get the button component if not assigned
        if (backButton == null)
        {
            backButton = GetComponent<Button>();
        }

        // Add listener
        if (backButton != null)
        {
            backButton.onClick.AddListener(GoBackToMenu);
        }
    }

    private void GoBackToMenu()
    {
        // Use the SessionRouter to handle scene transition
        SessionRouter.RouteToMainMenu();
    }

    private void OnDestroy()
    {
        // Clean up listener when destroyed
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(GoBackToMenu);
        }
    }
}

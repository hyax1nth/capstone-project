using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SessionRouter
{
    public static async Task RouteToAppropriateScene()
    {
        try
        {
            if (!AuthManager.Instance.IsSignedIn)
            {
                SceneManager.LoadScene(Constants.Scenes.MainMenu);
                return;
            }

            string userId = AuthManager.Instance.CurrentUserId;
            string role = await UserProfileService.Instance.GetRole(userId);

            if (role == "admin")
            {
                SceneManager.LoadScene(Constants.Scenes.AdminDashboard);
                return;
            }

            bool isProfileComplete = await UserProfileService.Instance.EnsureProfileCompleteness(userId);
            if (!isProfileComplete)
            {
                SceneManager.LoadScene(Constants.Scenes.OnboardingAge);
                return;
            }

            SceneManager.LoadScene(Constants.Scenes.StudentDashboard);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Routing error: {ex.Message}");
            SceneManager.LoadScene(Constants.Scenes.MainMenu);
        }
    }

    public static void RouteToMainMenu()
    {
        SceneManager.LoadScene(Constants.Scenes.MainMenu);
    }

    public static void RouteToSignIn()
    {
        SceneManager.LoadScene(Constants.Scenes.SignIn);
    }

    public static void RouteToSignUp()
    {
        SceneManager.LoadScene(Constants.Scenes.SignUp);
    }

    public static void RouteToOnboardingAge()
    {
        SceneManager.LoadScene(Constants.Scenes.OnboardingAge);
    }

    public static void RouteToOnboardingSubjects()
    {
        SceneManager.LoadScene(Constants.Scenes.OnboardingSubjects);
    }

    public static void RouteToStudentDashboard()
    {
        SceneManager.LoadScene(Constants.Scenes.StudentDashboard);
    }

    public static void RouteToAdminDashboard()
    {
        SceneManager.LoadScene(Constants.Scenes.AdminDashboard);
    }
}

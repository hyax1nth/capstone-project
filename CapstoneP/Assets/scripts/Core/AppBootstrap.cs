using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AppBootstrap : MonoBehaviour
{
    private void Start()
    {
        InitializeFirebaseAsync();
    }

    private async void InitializeFirebaseAsync()
    {
        try
        {
            Debug.Log("=================== FIREBASE INIT START ===================");
            Debug.Log("[BeeCool] Initializing Firebase...");
            
            // Check dependencies
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus != DependencyStatus.Available)
            {
                Debug.LogError("[BeeCool] Could not resolve Firebase dependencies: " + dependencyStatus);
                return;
            }
            
            Debug.Log("[BeeCool] Firebase dependencies ready!");

            // Enable offline persistence for Realtime Database
            FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(true);
            Debug.Log("[BeeCool] Firebase offline persistence enabled!");
            Debug.Log("=================== FIREBASE INIT SUCCESS ===================");

            // Ensure default catalog exists
            await CatalogService.Instance.EnsureDefaultCatalog();

            // Route to appropriate scene based on auth state
            await SessionRouter.RouteToAppropriateScene();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Firebase initialization error: {ex.Message}");
            SceneManager.LoadScene(Constants.Scenes.MainMenu);
        }
    }
}

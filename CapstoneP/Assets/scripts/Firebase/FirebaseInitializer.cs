using System;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Auth;

/// <summary>
/// Initialize Firebase and enable offline persistence before any DB refs are created.
/// Attach to a persistent GameObject in the first scene (e.g. Login/Bootstrap scene).
/// </summary>
public class FirebaseInitializer : MonoBehaviour
{
    public static bool IsInitialized { get; private set; }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    private void Initialize()
    {
    Debug.Log("FirebaseInitializer: Initialize called.");
        if (IsInitialized) return;

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var status = task.Result;
            Debug.Log("FirebaseInitializer: dependency check result = " + status);
            if (status == DependencyStatus.Available)
            {
                // Enable disk persistence for Realtime Database (must be before any DB usage)
                try
                {
                    FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(true);
                    Debug.Log("FirebaseInitializer: persistence enabled.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Could not enable persistence: " + ex.Message);
                }

                // Log app/database config for diagnostics
                try
                {
                    var app = FirebaseApp.DefaultInstance;
                    Debug.Log($"FirebaseInitializer: App name={app.Name} projectId={app.Options.ProjectId} dbUrl={app.Options.DatabaseUrl}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("FirebaseInitializer: could not read FirebaseApp options: " + ex.Message);
                }

                // For development: sign in anonymously in the Editor only (do not force anonymous sign-in in production)
#if UNITY_EDITOR
                try
                {
                    FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync().ContinueWith(t =>
                    {
                        if (t.IsCanceled || t.IsFaulted)
                        {
                            Debug.LogWarning("FirebaseInitializer: anonymous sign-in failed: " + (t.Exception != null ? t.Exception.Flatten().ToString() : "unknown"));
                        }
                        else
                        {
                            var user = t.Result.User;
                            Debug.Log("FirebaseInitializer: signed in anonymously uid=" + user.UserId);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("FirebaseInitializer: anonymous sign-in threw: " + ex.Message);
                }
#endif

                IsInitialized = true;
                Debug.Log("Firebase initialized and persistence enabled.");
            }
            else
            {
                Debug.LogError("Could not resolve Firebase dependencies: " + status);
            }
        });
    }
}

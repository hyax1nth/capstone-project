using System;
using UnityEngine;
using Firebase;
using Firebase.Database;

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

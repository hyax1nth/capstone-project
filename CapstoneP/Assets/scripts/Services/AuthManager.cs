using System;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Auth;

/// <summary>
/// Handles email signup/signin and exposes current user info.
/// Designed for scene hookup via Unity UI buttons/inputs.
/// </summary>
public class AuthManager : MonoBehaviour
{
    public static FirebaseAuth Auth => FirebaseAuth.DefaultInstance;
    public static FirebaseUser CurrentUser => Auth.CurrentUser;

    public event Action<FirebaseUser> OnAuthStateChanged;

    private void OnEnable()
    {
        // Ensure Firebase is initialized before subscribing to Auth state to avoid null refs
        if (FirebaseInitializer.IsInitialized)
        {
            Auth.StateChanged += HandleStateChanged;
        }
        else
        {
            Debug.Log("AuthManager: Firebase not initialized yet. Will subscribe when ready.");
            StartCoroutine(SubscribeWhenReady());
        }
    }

    private void OnDisable()
    {
        try { Auth.StateChanged -= HandleStateChanged; } catch { }
    }

    private System.Collections.IEnumerator SubscribeWhenReady()
    {
        float timeout = 10f;
        float t = 0f;
        while (t < timeout && !FirebaseInitializer.IsInitialized)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (FirebaseInitializer.IsInitialized)
        {
            Debug.Log("AuthManager: Firebase initialized — subscribing to Auth.StateChanged.");
            Auth.StateChanged += HandleStateChanged;
            // invoke initial state
            HandleStateChanged(this, EventArgs.Empty);
        }
        else
        {
            Debug.LogWarning("AuthManager: Firebase did not initialize in time; auth callbacks may not work.");
        }
    }

    private void HandleStateChanged(object sender, EventArgs args)
    {
        OnAuthStateChanged?.Invoke(CurrentUser);
    }

    public Task<FirebaseUser> SignUpWithEmail(string email, string password)
    {
        return SignUpWithEmailAsync(email, password);
    }

    public Task<FirebaseUser> SignInWithEmail(string email, string password)
    {
        return SignInWithEmailAsync(email, password);
    }

    private async Task<FirebaseUser> SignUpWithEmailAsync(string email, string password)
    {
        try
        {
            await Auth.CreateUserWithEmailAndPasswordAsync(email, password);
            return Auth.CurrentUser;
        }
        catch (Exception ex)
        {
            Debug.LogError("SignUp failed: " + ex);
            return null;
        }
    }

    private async Task<FirebaseUser> SignInWithEmailAsync(string email, string password)
    {
        try
        {
            await Auth.SignInWithEmailAndPasswordAsync(email, password);
            return Auth.CurrentUser;
        }
        catch (Exception ex)
        {
            Debug.LogError("SignIn failed: " + ex);
            return null;
        }
    }

    public void SignOut()
    {
        Auth.SignOut();
    }
}

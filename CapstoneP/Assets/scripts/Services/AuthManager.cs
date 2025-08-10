using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

public class AuthManager
{
    private static AuthManager _instance;
    public static AuthManager Instance => _instance ??= new AuthManager();

    private FirebaseAuth _auth;
    private bool _isInitialized;

    private AuthManager()
    {
        InitializeAuth();
    }

    private async void InitializeAuth()
    {
        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus == DependencyStatus.Available)
            {
                _auth = FirebaseAuth.DefaultInstance;
                _isInitialized = true;
                Debug.Log("Firebase Auth initialized successfully");
                
                // Set up auth state listener
                _auth.StateChanged += AuthStateChanged;
            }
            else
            {
                Debug.LogError($"Could not resolve Firebase dependencies: {dependencyStatus}");
                _isInitialized = false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error initializing Firebase Auth: {ex.Message}");
            _isInitialized = false;
        }
    }

    private void AuthStateChanged(object sender, EventArgs e)
    {
        if (_auth.CurrentUser != null)
        {
            Debug.Log($"User signed in: {_auth.CurrentUser.Email}");
        }
        else
        {
            Debug.Log("User signed out");
        }
    }

    public bool IsSignedIn => _auth?.CurrentUser != null;
    public string CurrentUserId => _auth?.CurrentUser?.UserId;

    public async Task<(bool success, string message)> SignInAsync(string email, string password)
    {
        if (!_isInitialized)
        {
            Debug.LogError("Auth not initialized. Reinitializing...");
            InitializeAuth();
            await Task.Delay(1000); // Give time for initialization
            if (!_isInitialized)
            {
                return (false, "Authentication service not available");
            }
        }

        try
        {
            await _auth.SignInWithEmailAndPasswordAsync(email, password)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted)
                    {
                        throw task.Exception;
                    }
                    return task.Result;
                });
            
            return (true, "Sign in successful!");
        }
        catch (Exception ex)
        {
            string message = GetFriendlyAuthErrorMessage(GetAuthErrorCode(ex));
            Debug.LogError($"Sign in error: {ex.Message}");
            return (false, message);
        }
    }

    public async Task<(bool success, string message)> SignUpAsync(string email, string password)
    {
        if (!_isInitialized)
        {
            Debug.LogError("Auth not initialized. Reinitializing...");
            InitializeAuth();
            await Task.Delay(1000); // Give time for initialization
            if (!_isInitialized)
            {
                return (false, "Authentication service not available");
            }
        }

        try
        {
            await _auth.CreateUserWithEmailAndPasswordAsync(email, password)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted)
                    {
                        throw task.Exception;
                    }
                    return task.Result;
                });
            
            return (true, "Account created successfully!");
        }
        catch (Exception ex)
        {
            string message = GetFriendlyAuthErrorMessage(GetAuthErrorCode(ex));
            Debug.LogError($"Sign up error: {ex.Message}");
            return (false, message);
        }
    }

    public async Task SignOutAsync()
    {
        try
        {
            _auth.SignOut();
            await Task.CompletedTask; // For consistency in async/await pattern
        }
        catch (Exception ex)
        {
            Debug.LogError($"Sign out error: {ex.Message}");
            throw;
        }
    }

    private AuthError GetAuthErrorCode(Exception ex)
    {
        if (ex is AggregateException aggEx)
        {
            foreach (var inner in aggEx.InnerExceptions)
            {
                if (inner is FirebaseException firebaseEx)
                {
                    return (AuthError)firebaseEx.ErrorCode;
                }
            }
        }
        return AuthError.InvalidEmail; // Default error code
    }

    private string GetFriendlyAuthErrorMessage(AuthError errorCode)
    {
        switch (errorCode)
        {
            case AuthError.MissingEmail:
                return "Please enter your email address.";
            case AuthError.MissingPassword:
                return "Please enter your password.";
            case AuthError.WeakPassword:
                return "Password should be at least 6 characters long.";
            case AuthError.EmailAlreadyInUse:
                return "This email is already registered. Please sign in instead.";
            case AuthError.InvalidEmail:
                return "Please enter a valid email address.";
            case AuthError.WrongPassword:
                return "Incorrect password. Please try again.";
            case AuthError.UserNotFound:
                return "Account not found. Please check your email or sign up.";
            case AuthError.UserDisabled:
                return "This account has been disabled. Please contact support.";
            case AuthError.TooManyRequests:
                return "Too many attempts. Please try again later.";
            default:
                return "An error occurred. Please try again later.";
        }
    }
}

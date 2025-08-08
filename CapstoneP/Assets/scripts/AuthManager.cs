using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Auth;
using System.Collections;

public class AuthManager : MonoBehaviour
{
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;
    public TMP_InputField signUpEmailInput;
    public TMP_InputField signUpPasswordInput;
    public TMP_InputField signUpFirstNameInput;
    public TMP_InputField signUpLastNameInput;
    public TMP_Text feedbackText;
    public UIManager uiManager;

    private FirebaseAuth auth;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
            }
            else
            {
                feedbackText.text = "Could not resolve all Firebase dependencies: " + dependencyStatus;
            }
        });
    }

    public void LoginUser()
    {
        string email = loginEmailInput.text;
        string password = loginPasswordInput.text;
        feedbackText.text = "Logging in...";
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                feedbackText.text = "Login failed: " + task.Exception.Message;
            }
            else
            {
                feedbackText.text = "Login successful!";
                // You can check for admin email here
                if (email == "admin@email.com") // Replace with your admin email
                {
                    // Show admin UI (implement in UIManager)
                }
                else
                {
                    uiManager.ShowStudentHome();
                }
            }
        }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
    }

    public void SignUpUser()
    {
        string email = signUpEmailInput.text;
        string password = signUpPasswordInput.text;
        string firstName = signUpFirstNameInput.text;
        string lastName = signUpLastNameInput.text;
        feedbackText.text = "Signing up...";
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                feedbackText.text = "Sign up failed: " + task.Exception.Message;
            }
            else
            {
                feedbackText.text = "Sign up successful!";
                uiManager.ShowStudentHome();
                // Save firstName and lastName to database if needed
            }
        }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
    }
}

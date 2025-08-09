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
    feedbackText.text = "Please wait, logging you in...";
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                feedbackText.text = "Login failed. Please check your email and password, or try again later.";
            }
            else
            {
                feedbackText.text = "Welcome back! Login successful.";
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
    feedbackText.text = "Creating your account, please wait...";
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                feedbackText.text = "Sign up failed. Please check your details or try again later.";
            }
            else
            {
                feedbackText.text = "Account created! Welcome to Bee Cool.";
                uiManager.ShowStudentHome();
                // Save firstName and lastName to database if needed
            }
        }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
    }
}

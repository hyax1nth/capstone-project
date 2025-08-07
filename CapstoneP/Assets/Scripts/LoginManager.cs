using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class LoginManager:MonoBehaviour
{
    public TMP_InputField InputEmail;
    public TMP_InputField InputPassword;
    public TMP_Text feedbackText;


    public void OnLoginButtonClicked()
    {
        string email = InputEmail.text;
        string password = InputPassword.text;
        feedbackText.text = "Logging in...";

        // Dummy check (replace with Firebase later)
        if (email == "admin" && password == "1234")
        {
            feedbackText.text = "Login successful!";
            // Load next scene or show main menu
        }
        else
        {
            feedbackText.text = "Invalid credentials.";
        }

    }
}

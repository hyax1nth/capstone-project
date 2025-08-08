using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject MainMenuPanel;
    public GameObject LoginPanel;
    public GameObject SignUpPanel;
    public GameObject StudentHomePanel;

    void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        MainMenuPanel.SetActive(true);
        LoginPanel.SetActive(false);
        SignUpPanel.SetActive(false);
        StudentHomePanel.SetActive(false);
    }

    public void ShowLogin()
    {
        MainMenuPanel.SetActive(false);
        LoginPanel.SetActive(true);
        SignUpPanel.SetActive(false);
        StudentHomePanel.SetActive(false);
    }

    public void ShowSignUp()
    {
        MainMenuPanel.SetActive(false);
        LoginPanel.SetActive(false);
        SignUpPanel.SetActive(true);
        StudentHomePanel.SetActive(false);
    }

    // Removed AgePanel and SubjectPanel logic

    public void ShowStudentHome()
    {
        MainMenuPanel.SetActive(false);
        LoginPanel.SetActive(false);
        SignUpPanel.SetActive(false);
        StudentHomePanel.SetActive(true);
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A small MonoBehaviour to simulate a lesson run and write results to Firebase via ProgressService.
/// Hook dropdowns and inputs in the inspector.
/// </summary>
public class LessonTester : MonoBehaviour
{
    public TMP_Dropdown subjectDropdown;
    public TMP_Dropdown lessonDropdown; // values like L1..L5
    public TMP_Dropdown lessonTypeDropdown; // e.g. tracing, quiz, etc.
    public TMP_InputField correctInput;
    public TMP_InputField incorrectInput;
    public TMP_InputField timeMsInput;
    public Button submitButton;

    public ProgressService progressService;
    public AuthManager authManager;

    private void Start()
    {
        submitButton.onClick.AddListener(Submit);
    }

    public async void Submit()
    {
        var user = AuthManager.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user.");
            return;
        }

        string subject = subjectDropdown.options[subjectDropdown.value].text;
        string lessonId = lessonDropdown.options[lessonDropdown.value].text;
        string lessonType = lessonTypeDropdown.options[lessonTypeDropdown.value].text;

        int correct = int.TryParse(correctInput.text, out var c) ? c : 0;
        int incorrect = int.TryParse(incorrectInput.text, out var ic) ? ic : 0;
        long timeMs = long.TryParse(timeMsInput.text, out var t) ? t : 0L;

        try
        {
            await progressService.WriteAttempt(user.UserId, subject, lessonId, lessonType, correct, incorrect, timeMs);
            Debug.Log("Attempt written.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to write attempt: " + ex.Message);
        }
    }
}

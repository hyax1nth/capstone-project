using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LessonSpeech : GameLessonBase
{
    [Header("UI References")]
    public TMP_Text PromptText;
    public TMP_Text ScoreText;
    public TMP_Text TimeText;
    public Button StartButton;
    public Button SimulateButton;

    private int attemptsRemaining = 5;
    private int successfulAttempts;
    private readonly string[] simulatedResults = { "Good", "Okay", "Try again" };
    private int currentSimulationIndex;

    protected override void Start()
    {
        base.Start();
        
        PromptText.gameObject.SetActive(false);
        SimulateButton.gameObject.SetActive(false);

        StartButton.onClick.AddListener(() => {
            StartButton.gameObject.SetActive(false);
            PromptText.gameObject.SetActive(true);
            SimulateButton.gameObject.SetActive(true);
            StartLesson();
        });

        SimulateButton.onClick.AddListener(OnSimulateSpeech);
        UpdateUI();
    }

    private void OnSimulateSpeech()
    {
        if (!IsGameActive || attemptsRemaining <= 0) return;

        string result = simulatedResults[currentSimulationIndex];
        currentSimulationIndex = (currentSimulationIndex + 1) % simulatedResults.Length;

        // Process result
        switch (result)
        {
            case "Good":
                successfulAttempts++;
                PromptText.text = "Excellent! Try another word.";
                break;
            case "Okay":
                successfulAttempts++;
                PromptText.text = "Good effort! Try another word.";
                break;
            case "Try again":
                PromptText.text = "Try saying it again.";
                break;
        }

        attemptsRemaining--;

        // Calculate stars based on successful attempts
        StarsAwarded = Mathf.CeilToInt((float)successfulAttempts / 5 * 3);
        Score = Mathf.RoundToInt((float)successfulAttempts / 5 * 100);

        if (attemptsRemaining <= 0)
        {
            EndLesson(true);
        }

        UpdateUI();
    }

    protected override void Update()
    {
        base.Update();
        if (IsGameActive)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        ScoreText.text = $"Attempts Left: {attemptsRemaining}";
        TimeText.text = $"Time: {Mathf.CeilToInt(RemainingTime)}s";
        SimulateButton.interactable = IsGameActive && attemptsRemaining > 0;
    }

    private void OnDestroy()
    {
        if (StartButton != null)
            StartButton.onClick.RemoveAllListeners();
        if (SimulateButton != null)
            SimulateButton.onClick.RemoveAllListeners();
    }
}

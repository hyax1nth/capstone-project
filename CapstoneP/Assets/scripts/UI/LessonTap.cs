using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LessonTap : GameLessonBase
{
    [Header("UI References")]
    public Button TapArea;
    public TMP_Text ScoreText;
    public TMP_Text TimeText;
    public Button StartButton;

    private int tapCount;
    private readonly int[] starThresholds = { 10, 20, 30 };

    protected override void Start()
    {
        base.Start();
        
        TapArea.gameObject.SetActive(false);
        StartButton.onClick.AddListener(() => {
            StartButton.gameObject.SetActive(false);
            TapArea.gameObject.SetActive(true);
            StartLesson();
        });

        TapArea.onClick.AddListener(OnTapArea);
        UpdateUI();
    }

    private void OnTapArea()
    {
        if (!IsGameActive) return;

        tapCount++;
        Score = Mathf.RoundToInt((float)tapCount / starThresholds[2] * 100);
        
        // Calculate stars based on thresholds
        StarsAwarded = 0;
        for (int i = 0; i < starThresholds.Length; i++)
        {
            if (tapCount >= starThresholds[i])
                StarsAwarded = i + 1;
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
        ScoreText.text = $"Taps: {tapCount}";
        TimeText.text = $"Time: {Mathf.CeilToInt(RemainingTime)}s";
    }

    private void OnDestroy()
    {
        if (StartButton != null)
            StartButton.onClick.RemoveAllListeners();
        if (TapArea != null)
            TapArea.onClick.RemoveAllListeners();
    }
}

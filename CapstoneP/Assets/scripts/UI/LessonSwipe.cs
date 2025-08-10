using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LessonSwipe : GameLessonBase
{
    [Header("UI References")]
    public TMP_Text DirectionText;
    public TMP_Text ScoreText;
    public TMP_Text TimeText;
    public Button StartButton;
    public RectTransform SwipeArea;

    private Vector2 touchStart;
    private readonly float minSwipeDistance = 50f;
    private string currentDirection;
    private int correctSwipes;
    private int totalSwipes;
    private readonly string[] directions = { "UP", "DOWN", "LEFT", "RIGHT" };
    private readonly float[] starThresholds = { 0.6f, 0.8f, 0.95f }; // Accuracy thresholds

    protected override void Start()
    {
        base.Start();
        
        DirectionText.gameObject.SetActive(false);
        SwipeArea.gameObject.SetActive(false);

        StartButton.onClick.AddListener(() => {
            StartButton.gameObject.SetActive(false);
            DirectionText.gameObject.SetActive(true);
            SwipeArea.gameObject.SetActive(true);
            StartLesson();
            SetNextDirection();
        });

        UpdateUI();
    }

    protected override void Update()
    {
        base.Update();
        if (!IsGameActive) return;

        UpdateUI();
        HandleSwipeInput();
    }

    private void HandleSwipeInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStart = touch.position;
                    break;

                case TouchPhase.Ended:
                    Vector2 swipeDelta = touch.position - touchStart;

                    if (swipeDelta.magnitude >= minSwipeDistance)
                    {
                        string swipeDirection = GetSwipeDirection(swipeDelta);
                        CheckSwipe(swipeDirection);
                    }
                    break;
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            touchStart = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            Vector2 swipeDelta = (Vector2)Input.mousePosition - touchStart;

            if (swipeDelta.magnitude >= minSwipeDistance)
            {
                string swipeDirection = GetSwipeDirection(swipeDelta);
                CheckSwipe(swipeDirection);
            }
        }
    }

    private string GetSwipeDirection(Vector2 swipe)
    {
        if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
        {
            return swipe.x > 0 ? "RIGHT" : "LEFT";
        }
        else
        {
            return swipe.y > 0 ? "UP" : "DOWN";
        }
    }

    private void CheckSwipe(string swipeDirection)
    {
        totalSwipes++;
        
        if (swipeDirection == currentDirection)
        {
            correctSwipes++;
        }

        float accuracy = (float)correctSwipes / totalSwipes;
        
        // Calculate stars based on accuracy
        StarsAwarded = 0;
        for (int i = 0; i < starThresholds.Length; i++)
        {
            if (accuracy >= starThresholds[i])
                StarsAwarded = i + 1;
        }

        Score = Mathf.RoundToInt(accuracy * 100);
        SetNextDirection();
    }

    private void SetNextDirection()
    {
        currentDirection = directions[Random.Range(0, directions.Length)];
        DirectionText.text = $"Swipe {currentDirection}";
    }

    private void UpdateUI()
    {
        ScoreText.text = totalSwipes > 0 
            ? $"Accuracy: {((float)correctSwipes / totalSwipes):P0}" 
            : "Swipe in the shown direction";
        TimeText.text = $"Time: {Mathf.CeilToInt(RemainingTime)}s";
    }

    private void OnDestroy()
    {
        if (StartButton != null)
            StartButton.onClick.RemoveAllListeners();
    }
}

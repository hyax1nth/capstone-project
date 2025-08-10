using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class LessonDrag : GameLessonBase
{
    [Header("UI References")]
    public GameObject DraggableItem;
    public Transform[] TargetZones;
    public TMP_Text ScoreText;
    public TMP_Text TimeText;
    public Button StartButton;

    private Vector2 originalPosition;
    private bool isDragging;
    private float accuracy;
    private int successfulDrops;
    private readonly float[] starThresholds = { 0.5f, 0.75f, 0.9f }; // Accuracy thresholds

    protected override void Start()
    {
        base.Start();
        
        originalPosition = DraggableItem.transform.position;
        DraggableItem.SetActive(false);

        StartButton.onClick.AddListener(() => {
            StartButton.gameObject.SetActive(false);
            DraggableItem.SetActive(true);
            StartLesson();
        });

        // Add drag handlers
        var dragHandler = DraggableItem.AddComponent<DragHandler>();
        dragHandler.OnBeginDragAction = OnBeginDrag;
        dragHandler.OnDragAction = OnDrag;
        dragHandler.OnEndDragAction = OnEndDrag;

        UpdateUI();
    }

    private void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsGameActive) return;
        isDragging = true;
    }

    private void OnDrag(PointerEventData eventData)
    {
        if (!IsGameActive || !isDragging) return;
        DraggableItem.transform.position = eventData.position;
    }

    private void OnEndDrag(PointerEventData eventData)
    {
        if (!IsGameActive) return;
        isDragging = false;

        bool successfulDrop = false;
        foreach (var zone in TargetZones)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                zone as RectTransform, 
                eventData.position))
            {
                successfulDrop = true;
                successfulDrops++;
                
                // Calculate accuracy based on distance to zone center
                var zoneCenter = zone.position;
                var dropDistance = Vector2.Distance(eventData.position, zoneCenter);
                var maxDistance = 100f; // Adjust based on your UI scale
                accuracy = Mathf.Max(accuracy, 1f - (dropDistance / maxDistance));
                
                break;
            }
        }

        if (!successfulDrop)
        {
            DraggableItem.transform.position = originalPosition;
        }

        // Calculate stars based on accuracy
        StarsAwarded = 0;
        for (int i = 0; i < starThresholds.Length; i++)
        {
            if (accuracy >= starThresholds[i])
                StarsAwarded = i + 1;
        }

        Score = Mathf.RoundToInt(accuracy * 100);
        UpdateUI();

        // Reset position for next attempt
        DraggableItem.transform.position = originalPosition;
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
        ScoreText.text = $"Accuracy: {accuracy:P0}";
        TimeText.text = $"Time: {Mathf.CeilToInt(RemainingTime)}s";
    }

    private void OnDestroy()
    {
        if (StartButton != null)
            StartButton.onClick.RemoveAllListeners();
    }
}

// Helper component for drag operations
public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public System.Action<PointerEventData> OnBeginDragAction;
    public System.Action<PointerEventData> OnDragAction;
    public System.Action<PointerEventData> OnEndDragAction;

    public void OnBeginDrag(PointerEventData eventData)
    {
        OnBeginDragAction?.Invoke(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        OnDragAction?.Invoke(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        OnEndDragAction?.Invoke(eventData);
    }
}

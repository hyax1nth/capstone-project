using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class ButtonPressAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Animation Settings")]
    [SerializeField] private float pressedYOffset = -5f; // How far down the button moves when pressed
    [SerializeField] private float animationSpeed = 15f; // Speed of the animation

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Vector2 pressedPosition;
    private bool stayPressed = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
        pressedPosition = originalPosition + new Vector2(0, pressedYOffset);
    }

    private void Update()
    {
        // Smoothly animate to target position
        Vector2 targetPosition = stayPressed ? pressedPosition : originalPosition;
        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition,
            targetPosition,
            Time.deltaTime * animationSpeed
        );
    }

    public void SetStayPressed(bool stay)
    {
        stayPressed = stay;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Temporary press effect when clicking
        if (!stayPressed)
        {
            rectTransform.anchoredPosition = pressedPosition;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Let the Update function handle the position
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Let the Update function handle the position
    }
}

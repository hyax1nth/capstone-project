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
    private bool isPressed = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
        pressedPosition = originalPosition + new Vector2(0, pressedYOffset);
    }

    private void Update()
    {
        // Smoothly animate to target position
        Vector2 targetPosition = isPressed ? pressedPosition : originalPosition;
        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition,
            targetPosition,
            Time.deltaTime * animationSpeed
        );
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPressed = false;
    }
}

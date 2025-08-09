using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float pressOffset = 5f;
    private RectTransform rectTransform;
    private Vector2 originalPosition;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        rectTransform.anchoredPosition = originalPosition - new Vector2(0, pressOffset);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        rectTransform.anchoredPosition = originalPosition;
    }
}

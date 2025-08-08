using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class TracingController : MonoBehaviour
{
    [SerializeField] private StrokeGuide currentGuide;
    [SerializeField] private Camera inputCamera;

    private void Awake()
    {
        if (inputCamera == null) inputCamera = Camera.main;

#if UNITY_2023_1_OR_NEWER
        if (currentGuide == null)
            currentGuide = FindFirstObjectByType<StrokeGuide>(FindObjectsInactive.Exclude);
#else
        if (currentGuide == null)
            currentGuide = FindObjectOfType<StrokeGuide>();
#endif
    }

    private void Update()
    {
        Vector2 worldPos;

        // Touch input (mobile)
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            TouchControl touch = Touchscreen.current.primaryTouch;

            switch (touch.phase.ReadValue())
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    worldPos = inputCamera.ScreenToWorldPoint(touch.position.ReadValue());
                    currentGuide.CheckTouchStart(worldPos);
                    break;

                case UnityEngine.InputSystem.TouchPhase.Moved:
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    worldPos = inputCamera.ScreenToWorldPoint(touch.position.ReadValue());
                    currentGuide.TrackStroke(worldPos);
                    break;

                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    worldPos = inputCamera.ScreenToWorldPoint(touch.position.ReadValue());
                    currentGuide.CheckTouchEnd(worldPos);
                    break;
            }

            return;
        }

        // Mouse input (Editor or Desktop)
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
                currentGuide.CheckTouchStart(inputCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue()));
            else if (Mouse.current.leftButton.isPressed)
                currentGuide.TrackStroke(inputCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue()));
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
                currentGuide.CheckTouchEnd(inputCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue()));
        }
    }
}

using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System;

public class StrokeGuide : MonoBehaviour
{
    [Header("Guide Refs")]
    [SerializeField] private SpriteRenderer guideRenderer;
    [SerializeField] private Collider2D areaCollider;
    [SerializeField] private CircleCollider2D startCollider;
    [SerializeField] private CircleCollider2D endCollider;

    [Header("Brush")]
    [SerializeField] private LineRenderer linePrefab;
    [SerializeField] private Transform brushContainer;
    [SerializeField] private float minPointDistance = 0.02f;

    [Header("Rules")]
    [SerializeField] private bool requireStartOnDot = true;
    [SerializeField] private bool enforcePath = true;
    [SerializeField] private float offPathGraceSeconds = 0.25f;

    [Header("Feedback")]
    [SerializeField] private bool fadeOutOnComplete = true;
    [SerializeField] private float fadeDuration = 0.25f;
    public UnityEvent onStrokeActivated;
    public UnityEvent onStrokeCompleted;

    [Header("Chaining")]
    [SerializeField] private StrokeGuide nextGuide;
    [SerializeField] private float delayBeforeNext = 0.5f;
    [SerializeField] private TracingController controller;

    private LineRenderer currentLine;
    private bool isTracing = false;
    private bool isCompleted = false;
    private float offPathTimer = 0f;
    private bool wasOffPath = false;
    private Color originalColor;

    private void Awake()
    {
        if (guideRenderer != null)
            originalColor = guideRenderer.color;

        if (areaCollider == null)
            areaCollider = GetComponent<Collider2D>();

        if (startCollider == null || endCollider == null)
        {
            foreach (Transform child in transform)
            {
                if (!child.CompareTag("TraceMarker")) continue;
                var circle = child.GetComponent<CircleCollider2D>();
                if (!circle) continue;
                string n = child.name.ToLower();
                if (n.Contains("start")) startCollider = circle;
                else if (n.Contains("end")) endCollider = circle;
            }
        }

#if UNITY_2023_1_OR_NEWER
        if (controller == null)
            controller = FindFirstObjectByType<TracingController>(FindObjectsInactive.Exclude);
#else
        if (controller == null)
            controller = FindObjectOfType<TracingController>();
#endif
    }

    private bool OverStart(Vector2 p) => startCollider != null && startCollider.OverlapPoint(p);
    private bool OverEnd(Vector2 p) => endCollider != null && endCollider.OverlapPoint(p);
    private bool OnPath(Vector2 p) => !enforcePath || (areaCollider != null && areaCollider.OverlapPoint(p));

    public void CheckTouchStart(Vector2 worldPos)
    {
        if (isCompleted) return;
        if (requireStartOnDot && !OverStart(worldPos)) return;
        if (!OnPath(worldPos)) return;

        isTracing = true;
        offPathTimer = 0f;
        wasOffPath = false;

        currentLine = Instantiate(linePrefab, brushContainer);
        currentLine.useWorldSpace = true;
        currentLine.alignment = LineAlignment.View;
        currentLine.sortingOrder = 200;
        currentLine.positionCount = 1;
        currentLine.SetPosition(0, worldPos);

        onStrokeActivated?.Invoke();
    }

    public void TrackStroke(Vector2 worldPos)
    {
        if (!isTracing || currentLine == null) return;

        if (enforcePath && areaCollider != null)
        {
            bool onPathNow = areaCollider.OverlapPoint(worldPos);
            if (!onPathNow)
            {
                offPathTimer += Time.deltaTime;
                wasOffPath = true;
                if (offPathTimer >= offPathGraceSeconds)
                {
                    ResetCurrentStroke();
                    return;
                }
            }
        }

        Vector3 last = currentLine.GetPosition(currentLine.positionCount - 1);
        if (Vector2.Distance(last, worldPos) >= minPointDistance)
        {
            int next = currentLine.positionCount + 1;
            currentLine.positionCount = next;
            currentLine.SetPosition(next - 1, worldPos);
        }

        if (OverEnd(worldPos))
            CompleteStroke();
    }

    private void ResetCurrentStroke()
    {
        Debug.Log($"[StrokeGuide:{name}] Stroke canceled — off path too long.");
        isTracing = false;
        offPathTimer = 0f;
        wasOffPath = false;

        if (currentLine != null)
            Destroy(currentLine.gameObject);

        currentLine = null;

        // Optional: play a soft "oops" sound or flash feedback
    }

    public void CheckTouchEnd(Vector2 worldPos)
    {
        if (!isTracing) return;

        if (OverEnd(worldPos))
        {
            CompleteStroke();
            return;
        }

        isTracing = false;
        currentLine = null;
        offPathTimer = 0f;
        wasOffPath = false;
    }

    private void CompleteStroke()
    {
        if (!isTracing) return;

        isTracing = false;
        isCompleted = true;
        currentLine = null;
        offPathTimer = 0f;
        wasOffPath = false;

        onStrokeCompleted?.Invoke();

        // Prevent retriggering
        if (areaCollider) areaCollider.enabled = false;
        if (startCollider) startCollider.enabled = false;
        if (endCollider) endCollider.enabled = false;

        // Orchestrate: handoff first, then fade/disable
        StartCoroutine(CompleteAndHandoff());
    }

    private IEnumerator CompleteAndHandoff()
    {
        // Wait for the configured delay before next
        if (delayBeforeNext > 0f)
            yield return new WaitForSeconds(delayBeforeNext);

        // Activate and hand off input before any self-disable
        if (nextGuide != null)
        {
            if (!nextGuide.gameObject.activeSelf)
                nextGuide.gameObject.SetActive(true);

            if (controller != null)
                controller.SetCurrentGuide(nextGuide);
        }

        // Now fade and disable this guide (optional)
        if (fadeOutOnComplete && guideRenderer != null)
            yield return FadeOut();

        gameObject.SetActive(false);
    }

    private IEnumerator FadeOut()
    {
        float t = 0f;
        Color start = guideRenderer.color;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            var c = start;
            c.a = Mathf.Lerp(start.a, 0f, k);
            guideRenderer.color = c;
            yield return null;
        }
    }
}
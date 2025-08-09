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

    [Header("Brush Cleanup On Complete")]
    [SerializeField] private bool fadeBrushOnComplete = false;
    [SerializeField] private float brushFadeDuration = 0.2f;

    [Header("Feedback")]
    [SerializeField] private bool fadeOutOnComplete = true;
    [SerializeField] private float fadeDuration = 0.25f;
    public UnityEvent onStrokeActivated;
    public UnityEvent onStrokeCompleted;

    [Header("Chaining")]
    [SerializeField] private StrokeGuide nextGuide;
    [SerializeField] private float delayBeforeNext = 0.5f;
    [SerializeField] private TracingController controller;

    [Header("Idle hint")]
    [SerializeField] private float idleHintDelay = 0.3f;
    [SerializeField] private IdleTraceHint idleHint;

    private LineRenderer currentLine;
    private bool isTracing = false;
    private bool isCompleted = false;
    private float offPathTimer = 0f;
    private bool wasOffPath = false;
    private Color originalColor;

    // Track endpoints hit during this trace
    private bool touchedStart = false;
    private bool touchedEnd = false;

    private void Awake()
    {
        if (idleHint == null) idleHint = GetComponent<IdleTraceHint>();

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

    private void OnEnable()
    {
        if (!isCompleted && idleHint != null)
            StartCoroutine(ShowHintDelayed());
    }

    private IEnumerator ShowHintDelayed()
    {
        yield return new WaitForSeconds(idleHintDelay);
        if (!isTracing && !isCompleted && idleHint != null)
            idleHint.Show();
    }

    private bool OverStart(Vector2 p) => startCollider != null && startCollider.OverlapPoint(p);
    private bool OverEnd(Vector2 p) => endCollider != null && endCollider.OverlapPoint(p);
    private bool OnPath(Vector2 p) => !enforcePath || (areaCollider != null && areaCollider.OverlapPoint(p));

    public void CheckTouchStart(Vector2 worldPos)
    {
        if (isCompleted) return;
        if (requireStartOnDot && !OverStart(worldPos)) return;
        if (!OnPath(worldPos)) return;

        // Hide the idle hint the moment the user begins a valid trace
        if (idleHint != null) idleHint.Hide();

        isTracing = true;
        offPathTimer = 0f;
        wasOffPath = false;

        // mark start if they truly began on it
        touchedStart = OverStart(worldPos);
        touchedEnd = false;

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
            else
            {
                // reset grace if they returned on path
                offPathTimer = 0f;
                wasOffPath = false;
            }
        }

        Vector3 last = currentLine.GetPosition(currentLine.positionCount - 1);
        if (Vector2.Distance(last, worldPos) >= minPointDistance)
        {
            int next = currentLine.positionCount + 1;
            currentLine.positionCount = next;
            currentLine.SetPosition(next - 1, worldPos);
        }

        // Track endpoint touches
        if (OverStart(worldPos)) touchedStart = true;
        if (OverEnd(worldPos)) touchedEnd = true;

        // Complete only after start has been touched and we reach end
        if (touchedStart && OverEnd(worldPos))
            CompleteStroke();
    }

    private void ResetCurrentStroke()
    {
        Debug.Log($"[StrokeGuide:{name}] Stroke canceled — off path too long.");
        isTracing = false;
        offPathTimer = 0f;
        wasOffPath = false;
        touchedStart = false;
        touchedEnd = false;

        if (currentLine != null)
            Destroy(currentLine.gameObject);

        currentLine = null;

        // Optionally reshow hint after a cancel (kid-friendly nudge)
        if (!isCompleted && idleHint != null)
            StartCoroutine(ShowHintDelayed());
    }

    public void CheckTouchEnd(Vector2 worldPos)
    {
        if (!isTracing) return;

        // Allow lift-to-complete if they ended on the end dot and have touched start
        if (touchedStart && OverEnd(worldPos))
        {
            CompleteStroke();
            return;
        }

        // User lifted without completing: clear the partial stroke
        isTracing = false;
        offPathTimer = 0f;
        wasOffPath = false;
        touchedStart = false;
        touchedEnd = false;

        if (currentLine != null)
            Destroy(currentLine.gameObject);
        currentLine = null;

        // Optionally show hint again
        if (!isCompleted && idleHint != null)
            StartCoroutine(ShowHintDelayed());
    }

    private void CompleteStroke()
    {
        if (!isTracing) return;

        isTracing = false;
        isCompleted = true;

        // Clear brush BEFORE nulling the reference
        if (currentLine != null)
        {
            if (fadeBrushOnComplete)
                StartCoroutine(FadeOutLineAndDestroy(currentLine, brushFadeDuration));
            else
                Destroy(currentLine.gameObject);
        }
        currentLine = null;

        offPathTimer = 0f;
        wasOffPath = false;
        touchedStart = false;
        touchedEnd = false;

        onStrokeCompleted?.Invoke();

        // Reveal fill
        Transform fill = transform.Find("HoleFill");
        if (fill != null)
        {
            fill.gameObject.SetActive(true);
            if (fill.TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f);
                StartCoroutine(FadeInFill(sr));
            }
        }

        // Prevent retriggering
        if (areaCollider) areaCollider.enabled = false;
        if (startCollider) startCollider.enabled = false;
        if (endCollider) endCollider.enabled = false;

        // Ensure hint is hidden on completion
        if (idleHint != null) idleHint.Hide();

        // Orchestrate: handoff first, then fade/disable
        StartCoroutine(CompleteAndHandoff());
    }

    private IEnumerator CompleteAndHandoff()
    {
        // Wait for the configured delay before next
        if (delayBeforeNext > 0f)
            yield return new WaitForSeconds(delayBeforeNext);

        // Activate and hand off input
        if (nextGuide != null)
        {
            if (!nextGuide.gameObject.activeSelf)
                nextGuide.gameObject.SetActive(true);

            if (controller != null)
                controller.SetCurrentGuide(nextGuide);

            // Show idle hint on the new active guide
            if (nextGuide.idleHint != null)
                nextGuide.idleHint.Show();
        }

        // Now fade and disable this guide (optional)
        if (fadeOutOnComplete && guideRenderer != null)
            yield return FadeOut();

        gameObject.SetActive(false);
    }

    private IEnumerator FadeInFill(SpriteRenderer sr)
    {
        float t = 0f;
        float duration = 0.3f;
        Color start = sr.color;
        Color target = new Color(start.r, start.g, start.b, 1f);

        while (t < duration)
        {
            t += Time.deltaTime;
            sr.color = Color.Lerp(start, target, t / duration);
            yield return null;
        }
        sr.color = target;
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
        var done = guideRenderer.color;
        done.a = 0f;
        guideRenderer.color = done;
    }

    private IEnumerator FadeOutLineAndDestroy(LineRenderer lr, float duration)
    {
        if (lr == null) yield break;

        float t = 0f;

        // Cache initial colors (works if no gradient; for gradients, swap to color gradient logic)
        var startColor = lr.startColor;
        var endColor = lr.endColor;

        while (t < duration && lr != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float a = Mathf.Lerp(1f, 0f, k);
            var sc = startColor; sc.a *= a;
            var ec = endColor; ec.a *= a;
            lr.startColor = sc;
            lr.endColor = ec;
            yield return null;
        }

        if (lr != null)
            Destroy(lr.gameObject);
    }
}
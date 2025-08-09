using UnityEngine;
using UnityEngine.Events;
using System.Collections;

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
    private bool wasOffPath = false; // for logging transitions
    private Color originalColor;

    private void Awake()
    {
        if (guideRenderer != null)
            originalColor = guideRenderer.color;

        if (areaCollider == null)
        {
            areaCollider = GetComponent<Collider2D>();
            if (areaCollider == null)
                Debug.LogWarning($"[StrokeGuide:{name}] No areaCollider assigned or found. Path enforcement will be disabled.");
        }

        if (startCollider == null || endCollider == null)
        {
            foreach (Transform child in transform)
            {
                if (child.CompareTag("TraceMarker"))
                {
                    var circle = child.GetComponent<CircleCollider2D>();
                    if (circle == null) continue;
                    string n = child.name.ToLower();
                    if (n.Contains("start")) startCollider = circle;
                    else if (n.Contains("end")) endCollider = circle;
                }
            }
        }

        if (startCollider == null)
            Debug.LogWarning($"[StrokeGuide:{name}] Start collider missing. Assign G*_StartPoint in Inspector.");
        if (endCollider == null)
            Debug.LogWarning($"[StrokeGuide:{name}] End collider missing. Assign G*_EndPoint in Inspector.");

        // Auto-find controller if unassigned
        if (controller == null)
        {
#if UNITY_2023_1_OR_NEWER
            controller = FindFirstObjectByType<TracingController>(FindObjectsInactive.Exclude);
#else
            controller = FindObjectOfType<TracingController>();
#endif
            if (controller == null)
                Debug.LogWarning($"[StrokeGuide:{name}] TracingController not found. Handoff will not occur.");
        }
    }

    private bool OverStart(Vector2 p)
    {
        if (startCollider == null) return false;
        return startCollider.OverlapPoint(p);
    }

    private bool OverEnd(Vector2 p)
    {
        if (endCollider == null) return false;
        return endCollider.OverlapPoint(p);
    }

    private bool OnPath(Vector2 p) => !enforcePath || (areaCollider != null && areaCollider.OverlapPoint(p));

    public void CheckTouchStart(Vector2 worldPos)
    {
        if (isCompleted)
        {
            Debug.Log($"[StrokeGuide:{name}] Ignoring start — stroke already completed.");
            return;
        }
        if (requireStartOnDot && !OverStart(worldPos))
        {
            Debug.Log($"[StrokeGuide:{name}] Touch start rejected (not on start dot).");
            return;
        }
        if (!OnPath(worldPos))
        {
            Debug.Log($"[StrokeGuide:{name}] Touch start rejected (not on path).");
            return;
        }

        isTracing = true;
        offPathTimer = 0f;
        wasOffPath = false;

        currentLine = Instantiate(linePrefab, brushContainer);
        currentLine.useWorldSpace = true;
        currentLine.alignment = LineAlignment.View;
        currentLine.sortingOrder = 200;
        currentLine.positionCount = 1;
        currentLine.SetPosition(0, worldPos);

        Debug.Log($"[StrokeGuide:{name}] Stroke started at {worldPos}. Line instance: {currentLine.name}");
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
                if (!wasOffPath)
                {
                    wasOffPath = true;
                    Debug.Log($"[StrokeGuide:{name}] Off path… starting grace timer.");
                }
                if (offPathTimer >= offPathGraceSeconds)
                    return; // pause adding points
            }
            else
            {
                if (wasOffPath)
                {
                    Debug.Log($"[StrokeGuide:{name}] Back on path. Grace timer reset.");
                    wasOffPath = false;
                }
                offPathTimer = 0f;
            }
        }

        Vector3 last = currentLine.GetPosition(currentLine.positionCount - 1);
        if (Vector2.Distance(last, worldPos) >= minPointDistance)
        {
            int next = currentLine.positionCount + 1;
            currentLine.positionCount = next;
            currentLine.SetPosition(next - 1, worldPos);
            // Occasional breadcrumb log
            if (next % 10 == 0)
                Debug.Log($"[StrokeGuide:{name}] Points: {next}");
        }

        if (OverEnd(worldPos))
        {
            Debug.Log($"[StrokeGuide:{name}] End dot reached at {worldPos}.");
            CompleteStroke();
        }
    }

    public void CheckTouchEnd(Vector2 worldPos)
    {
        if (!isTracing) return;

        if (OverEnd(worldPos))
        {
            Debug.Log($"[StrokeGuide:{name}] Touch ended on end dot — completing stroke.");
            CompleteStroke();
            return;
        }

        Debug.Log($"[StrokeGuide:{name}] Touch ended early — canceling stroke.");
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

        Debug.Log($"[StrokeGuide:{name}] Stroke completed. Invoking events and triggering next guide.");
        onStrokeCompleted?.Invoke();

        // Disable colliders to prevent retriggering this guide
        if (areaCollider) areaCollider.enabled = false;
        if (startCollider) startCollider.enabled = false;
        if (endCollider) endCollider.enabled = false;

        TriggerNextGuide();

        if (fadeOutOnComplete && guideRenderer != null)
            StartCoroutine(FadeOutAndDisable());
        else
            gameObject.SetActive(false);
    }

    private void TriggerNextGuide()
    {
        if (nextGuide == null)
        {
            Debug.Log($"[StrokeGuide:{name}] No next guide assigned — this might be the final stroke.");
            return;
        }

        Debug.Log($"[StrokeGuide:{name}] Scheduling activation of next guide: {nextGuide.name} in {delayBeforeNext:0.00}s");
        StartCoroutine(ActivateNextAfterDelay());
    }

    private IEnumerator ActivateNextAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeNext);

        if (!nextGuide.gameObject.activeSelf)
        {
            nextGuide.gameObject.SetActive(true);
            Debug.Log($"[StrokeGuide:{name}] Activated next guide: {nextGuide.name}");
        }

        if (controller != null)
        {
            controller.SetCurrentGuide(nextGuide);
            Debug.Log($"[StrokeGuide:{name}] Handed off to next guide: {nextGuide.name}");
        }
        else
        {
            Debug.LogWarning($"[StrokeGuide:{name}] No TracingController assigned — cannot hand off input.");
        }
    }

    private IEnumerator FadeOutAndDisable()
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
        Debug.Log($"[StrokeGuide:{name}] Guide visuals faded out. Disabling object.");
        gameObject.SetActive(false);
    }
}
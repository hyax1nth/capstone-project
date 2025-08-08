using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class StrokeGuide : MonoBehaviour
{
    [Header("Guide Refs")]
    [SerializeField] private SpriteRenderer guideRenderer;   // Optional, for fade
    [SerializeField] private Collider2D areaCollider;        // PolygonCollider2D (Is Trigger)
    [SerializeField] private CircleCollider2D startCollider; // Child, Is Trigger, tag = TraceMarker
    [SerializeField] private CircleCollider2D endCollider;   // Child, Is Trigger, tag = TraceMarker

    [Header("Brush")]
    [SerializeField] private LineRenderer linePrefab;        // Drag your Line prefab
    [SerializeField] private Transform brushContainer;       // Drag BrushContainer
    [SerializeField] private float minPointDistance = 0.02f; // Spacing between line points

    [Header("Rules")]
    [SerializeField] private bool requireStartOnDot = true;
    [SerializeField] private bool enforcePath = true;
    [SerializeField] private float offPathGraceSeconds = 0.25f;

    [Header("Feedback")]
    [SerializeField] private bool fadeOutOnComplete = true;
    [SerializeField] private float fadeDuration = 0.25f;
    public UnityEvent onStrokeActivated;
    public UnityEvent onStrokeCompleted;

    private LineRenderer currentLine;
    private bool isTracing = false;
    private float offPathTimer = 0f;
    private Color originalColor;

    private void Awake()
    {
        if (guideRenderer != null) originalColor = guideRenderer.color;

        // Auto-assign by tag if not set
        if ((startCollider == null || endCollider == null))
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

        if (areaCollider == null)
            areaCollider = GetComponent<Collider2D>(); // Expect PolygonCollider2D here
    }

    private bool OverStart(Vector2 p) => startCollider == null || startCollider.OverlapPoint(p);
    private bool OverEnd(Vector2 p) => endCollider != null && endCollider.OverlapPoint(p);
    private bool OnPath(Vector2 p) => !enforcePath || (areaCollider != null && areaCollider.OverlapPoint(p));

    public void CheckTouchStart(Vector2 worldPos)
    {
        // Must start on StartPoint (if required) and be on path (if enforced)
        if (requireStartOnDot && !OverStart(worldPos)) return;
        if (!OnPath(worldPos)) return;

        isTracing = true;
        offPathTimer = 0f;

        // Start brush
        currentLine = Instantiate(linePrefab, brushContainer);
        currentLine.positionCount = 1;
        currentLine.SetPosition(0, worldPos);


        onStrokeActivated?.Invoke();
    }

    public void TrackStroke(Vector2 worldPos)
    {
        if (!isTracing || currentLine == null) return;

        // Path enforcement with grace period
        if (enforcePath && areaCollider != null)
        {
            if (!areaCollider.OverlapPoint(worldPos))
            {
                offPathTimer += Time.deltaTime;
                if (offPathTimer >= offPathGraceSeconds)
                    return; // Pause adding points until back on path
            }
            else
            {
                offPathTimer = 0f;
            }
        }

        Vector3 last = currentLine.positionCount > 0 ? currentLine.GetPosition(currentLine.positionCount - 1) : (Vector3)worldPos;
        if (Vector2.Distance(last, worldPos) >= minPointDistance)
        {
            int next = currentLine.positionCount + 1;
            currentLine.positionCount = next;
            currentLine.SetPosition(next - 1, worldPos);
        }

        // Success when finger reaches EndPoint
        if (OverEnd(worldPos))
            CompleteStroke();
    }

    public void CheckTouchEnd(Vector2 worldPos)
    {
        if (!isTracing) return;

        // Optional: allow end on release if near end marker
        if (OverEnd(worldPos))
        {
            CompleteStroke();
            return;
        }

        // Otherwise just stop drawing (did not complete)
        isTracing = false;
        currentLine = null;
        offPathTimer = 0f;
    }

    private void CompleteStroke()
    {
        if (!isTracing) return;

        isTracing = false;
        currentLine = null;
        offPathTimer = 0f;

        onStrokeCompleted?.Invoke();
        TriggerNextGuide(); // ✅ activate next if set

        if (fadeOutOnComplete && guideRenderer != null)
            StartCoroutine(FadeOutAndDisable());
    }



    private IEnumerator FadeOutAndDisable()
    {
        float t = 0f;
        Color start = guideRenderer.color;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            var c = start; c.a = Mathf.Lerp(start.a, 0f, k);
            guideRenderer.color = c;
            yield return null;
        }
        gameObject.SetActive(false);
    }

    [Header("Next Guide")]
    [SerializeField] private StrokeGuide nextGuide;
    [SerializeField] private float delayBeforeNext = 0.5f;

    private void TriggerNextGuide()
    {
        if (nextGuide != null)
            StartCoroutine(ActivateNextAfterDelay());
    }

    private IEnumerator ActivateNextAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeNext);
        nextGuide.gameObject.SetActive(true);
    }

}

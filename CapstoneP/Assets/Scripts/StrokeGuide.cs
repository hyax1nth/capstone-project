// Full code starts here — minimal, complete, and ownership-aware

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

    [Header("Hint ownership")]
    [SerializeField] private bool isStarter = false;
    private static StrokeGuide currentHintOwner;
    private bool suppressOnEnableHint = false;
    private bool IsOwner => currentHintOwner == this;

    private LineRenderer currentLine;
    private bool isTracing = false;
    private bool isCompleted = false;
    private float offPathTimer = 0f;
    private bool wasOffPath = false;

    private bool touchedStart = false;
    private bool touchedEnd = false;
    private Coroutine hintDelayRoutine;
    private Color originalColor;

    private void Awake()
    {
        if (idleHint == null) idleHint = GetComponent<IdleTraceHint>();
        if (guideRenderer != null) originalColor = guideRenderer.color;
        if (areaCollider == null) areaCollider = GetComponent<Collider2D>();

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
        if (currentHintOwner == null && isStarter)
            currentHintOwner = this;

        if (!isCompleted && idleHint != null && !suppressOnEnableHint && IsOwner)
            hintDelayRoutine = StartCoroutine(ShowHintDelayed());
    }

    private void OnDisable()
    {
        CancelHintDelay();
        if (isTracing || currentLine != null)
        {
            if (currentLine != null) Destroy(currentLine.gameObject);
            currentLine = null;
            isTracing = false;
        }

        if (IsOwner) currentHintOwner = null;
    }

    private IEnumerator ShowHintDelayed()
    {
        yield return new WaitForSeconds(idleHintDelay);
        if (IsOwner && !isTracing && !isCompleted && idleHint != null)
            idleHint.Show();
        suppressOnEnableHint = false;
        hintDelayRoutine = null;
    }

    private void CancelHintDelay()
    {
        if (hintDelayRoutine != null)
        {
            StopCoroutine(hintDelayRoutine);
            hintDelayRoutine = null;
        }
    }

    private bool OverStart(Vector2 p) => startCollider != null && startCollider.OverlapPoint(p);
    private bool OverEnd(Vector2 p) => endCollider != null && endCollider.OverlapPoint(p);
    private bool OnPath(Vector2 p) => !enforcePath || (areaCollider != null && areaCollider.OverlapPoint(p));

    public void CheckTouchStart(Vector2 worldPos)
    {
        if (isCompleted) return;
        if (requireStartOnDot && !OverStart(worldPos)) return;
        if (!OnPath(worldPos)) return;

        CancelHintDelay();
        if (idleHint != null) idleHint.Hide();

        isTracing = true;
        offPathTimer = 0f;
        wasOffPath = false;
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

        if (OverStart(worldPos)) touchedStart = true;
        if (OverEnd(worldPos)) touchedEnd = true;

        if (touchedStart && OverEnd(worldPos))
            CompleteStroke();
    }

    public void CheckTouchEnd(Vector2 worldPos)
    {
        if (!isTracing) return;

        if (touchedStart && OverEnd(worldPos))
        {
            CompleteStroke();
            return;
        }

        isTracing = false;
        offPathTimer = 0f;
        wasOffPath = false;
        touchedStart = false;
        touchedEnd = false;

        if (currentLine != null)
            Destroy(currentLine.gameObject);
        currentLine = null;

        if (!isCompleted && idleHint != null && IsOwner)
        {
            CancelHintDelay();
            hintDelayRoutine = StartCoroutine(ShowHintDelayed());
        }
    }

    private void ResetCurrentStroke()
    {
        isTracing = false;
        offPathTimer = 0f;
        wasOffPath = false;
        touchedStart = false;
        touchedEnd = false;

        if (currentLine != null)
            Destroy(currentLine.gameObject);
        currentLine = null;

        if (!isCompleted && idleHint != null && IsOwner)
        {
            CancelHintDelay();
            hintDelayRoutine = StartCoroutine(ShowHintDelayed());
        }
    }

    private void CompleteStroke()
    {
        if (!isTracing) return;

        isTracing = false;
        isCompleted = true;

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

        CancelHintDelay();
        onStrokeCompleted?.Invoke();
        controller?.NotifyGuideCompleted(this);

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

        if (areaCollider) areaCollider.enabled = false;
        if (startCollider) startCollider.enabled = false;
        if (endCollider) endCollider.enabled = false;

        if (idleHint != null) idleHint.Hide();

        StartCoroutine(CompleteAndHandoff());
    }

    private IEnumerator CompleteAndHandoff()
    {
        if (delayBeforeNext > 0f)
            yield return new WaitForSeconds(delayBeforeNext);

        if (nextGuide != null)
        {
            if (IsOwner) currentHintOwner = nextGuide;
            nextGuide.suppressOnEnableHint = true;
            controller?.SetCurrentGuide(nextGuide);
            nextGuide.ShowHintImmediately();
        }

        if (fadeOutOnComplete && guideRenderer != null)
            yield return FadeOut();

        DisableGuideInteraction();
    }

    private void DisableGuideInteraction()
    {
        if (areaCollider) areaCollider.enabled = false;
        if (startCollider) startCollider.enabled = false;
        if (endCollider) endCollider.enabled = false;

        if (idleHint != null)
        {
            idleHint.Hide();
            idleHint.enabled = false;
        }

        isTracing = false;
    }

    public void ShowHintImmediately()
    {
        if (idleHint == null || isCompleted) return;
        if (!IsOwner || isTracing) return;

        CancelHintDelay();
        idleHint.Show();
    }

    public void PlayHint()
    {
        if (idleHint == null || !IsOwner || isCompleted) return;
        CancelHintDelay();
        idleHint.Show();
    }

    public void StopHint()
    {
        if (idleHint == null) return;
        idleHint.Hide();
        idleHint.enabled = false;
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
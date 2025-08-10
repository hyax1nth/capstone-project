using UnityEngine;
using System.Collections;

public class IdleTraceHint : MonoBehaviour
{
    [Header("Hint & Path")]
    public Transform hint;                 // Hand/finger sprite transform
    public Transform waypointsRoot;        // Parent of WP_00, WP_01, ...

    [Header("Timing")]
    public float hintSpeed = 2.0f;         // Units per second along the path
    public float fadeDuration = 0.5f;      // Fade in/out time
    public float pauseDuration = 1.0f;     // Pause at the end before restarting

    [Header("Visuals")]
    public float offsetDistance = 0.2f;    // Fixed perpendicular offset from path (XY plane)
    public float fixedZRotation = 50f;     // Locked hand rotation

    [Header("Playback")]
    public bool playOnStart = true;        // Auto-start on Start()
    public bool loop = true;               // Repeat forever

    private Transform[] waypoints;
    private SpriteRenderer hintRenderer;
    private Coroutine runRoutine;

    void Awake()
    {
        if (!hint) hint = transform;
        hintRenderer = hint.GetComponent<SpriteRenderer>();
        if (!hintRenderer) hintRenderer = hint.GetComponentInChildren<SpriteRenderer>(true);

        // Cache waypoints in child order
        if (waypointsRoot)
        {
            int count = waypointsRoot.childCount;
            waypoints = new Transform[count];
            for (int i = 0; i < count; i++)
                waypoints[i] = waypointsRoot.GetChild(i);
        }
        else
        {
            waypoints = new Transform[0];
        }

        if (hint) hint.gameObject.SetActive(false);
    }

    void Start()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        // Place at the start-of-path with offset derived from first segment
        Vector3 startPos = GetSegmentAdjustedPointAtStart();
        hint.position = startPos;
        hint.rotation = Quaternion.Euler(0f, 0f, fixedZRotation);

        // Ensure visible alpha (we’ll control fades)
        SetAlpha(1f);

        if (playOnStart)
            runRoutine = StartCoroutine(RunLoop());
    }

    public void Show()
    {
        if (hint == null) return;
        hint.gameObject.SetActive(true);
        SetAlpha(0f); // start transparent
        if (runRoutine != null) StopCoroutine(runRoutine);
        runRoutine = StartCoroutine(RunLoop());
    }

    public void Hide()
    {
        if (hint) hint.gameObject.SetActive(false);
        if (runRoutine != null) StopCoroutine(runRoutine);
        runRoutine = null;
    }

    public void Restart()
    {
        if (runRoutine != null) StopCoroutine(runRoutine);
        runRoutine = StartCoroutine(RunLoop());
    }

    void OnDisable()
    {
        if (runRoutine != null) StopCoroutine(runRoutine);
    }

    IEnumerator RunLoop()
    {
        // Safety
        if (waypoints.Length < 2 || hintSpeed <= 0f)
            yield break;

        // Optional: start with fade in if you want
        yield return FadeTo(1f, fadeDuration);

        do
        {
            // Traverse each segment with a stable, precomputed perpendicular offset
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                Vector3 a = waypoints[i].position;
                Vector3 b = waypoints[i + 1].position;

                Vector3 dir = b - a;
                float len = dir.magnitude;
                if (len < 0.0001f)
                    continue; // skip degenerate segment

                Vector3 n = dir / len;
                Vector3 perp = new Vector3(-n.y, n.x, 0f); // XY-plane perpendicular
                Vector3 offset = perp * offsetDistance;

                Vector3 from = a + offset;
                Vector3 to = b + offset;

                float duration = Mathf.Max(0.0001f, len / hintSpeed);
                float t = 0f;

                // Lock rotation; we’re not aligning to path
                hint.rotation = Quaternion.Euler(0f, 0f, fixedZRotation);

                // Glide linearly from 'from' to 'to'
                while (t < 1f)
                {
                    hint.position = Vector3.Lerp(from, to, t);
                    t += Time.deltaTime / duration;
                    yield return null;
                }
                hint.position = to; // ensure exact end
            }

            // Fade out at end, pause, reset to start, fade in
            yield return FadeTo(0f, fadeDuration);
            yield return new WaitForSeconds(pauseDuration);

            // Reset to the same offset-relative start
            hint.position = GetSegmentAdjustedPointAtStart();
            hint.rotation = Quaternion.Euler(0f, 0f, fixedZRotation);

            yield return FadeTo(1f, fadeDuration);

        } while (loop);
    }

    Vector3 GetSegmentAdjustedPointAtStart()
    {
        if (waypoints.Length == 0) return hint.position;

        if (waypoints.Length == 1)
            return waypoints[0].position;

        Vector3 a = waypoints[0].position;
        Vector3 b = waypoints[1].position;
        Vector3 dir = b - a;
        float len = dir.magnitude;

        if (len < 0.0001f)
            return a;

        Vector3 n = dir / len;
        Vector3 perp = new Vector3(-n.y, n.x, 0f);
        return a + perp * offsetDistance;
    }

    IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (!hintRenderer || duration <= 0f)
        {
            SetAlpha(targetAlpha);
            yield break;
        }

        float startAlpha = hintRenderer.color.a;
        float t = 0f;
        while (t < 1f)
        {
            float a = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetAlpha(a);
            t += Time.deltaTime / duration;
            yield return null;
        }
        SetAlpha(targetAlpha);
    }

    void SetAlpha(float a)
    {
        if (!hintRenderer) return;
        Color c = hintRenderer.color;
        c.a = a;
        hintRenderer.color = c;
    }

#if UNITY_EDITOR
    // Optional gizmos to visualize the offset path in Scene view
    void OnDrawGizmosSelected()
    {
        if (!waypointsRoot || waypointsRoot.childCount < 2) return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        for (int i = 0; i < waypointsRoot.childCount - 1; i++)
        {
            Vector3 a = waypointsRoot.GetChild(i).position;
            Vector3 b = waypointsRoot.GetChild(i + 1).position;

            Vector3 dir = b - a;
            float len = dir.magnitude;
            if (len < 0.0001f) continue;

            Vector3 n = dir / Mathf.Max(0.0001f, len);
            Vector3 perp = new Vector3(-n.y, n.x, 0f);
            Vector3 off = perp * offsetDistance;

            Vector3 from = a + off;
            Vector3 to = b + off;

            Gizmos.DrawLine(from, to);
        }
    }
#endif
}
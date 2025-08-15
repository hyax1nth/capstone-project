using UnityEngine;
using System.Collections;

public class IdleTraceHint : MonoBehaviour
{
    public enum PathSource { WaypointTransforms, EdgeColliderPoints }
    public enum LoopMode { Once, Loop, PingPong }
    public enum StartAnchor { Start, End }

    [Header("Hint & Path")]
    public Transform hint;                  // the hand/pointer transform
    public PathSource pathSource = PathSource.EdgeColliderPoints;

    [Header("Waypoints (Transforms)")]
    public Transform waypointsRoot;         // parent with WP_00, WP_01, ...

    [Header("Edge Path")]
    public EdgeCollider2D edge;             // source curve for points
    public bool closeLoop = false;          // close path (useful for letter O)

    [Header("Timing & Motion")]
    public float hintSpeed = 2.0f;          // units per second
    public float fadeDuration = 0.25f;
    public float pauseDuration = 0.6f;      // pause at ends
    public LoopMode loopMode = LoopMode.Once;
    public StartAnchor startAnchor = StartAnchor.Start;

    [Header("Visuals")]
    public float offsetDistance = 0.15f;    // perpendicular offset from path
    public bool alignRotationToPath = false;
    public float fixedZRotation = 50f;      // used if not aligning

    [Header("Playback")]
    public bool playOnStart = false;        // keep false; StrokeGuide calls Show()
    public bool loop = true;                // legacy; ignored—use loopMode instead

    private SpriteRenderer hintRenderer;
    private Coroutine runRoutine;
    private Vector3[] pathWorld;            // baked world-space points

    void Awake()
    {
        if (!hint) hint = transform;
        hintRenderer = hint.GetComponent<SpriteRenderer>();
        if (!hintRenderer) hintRenderer = hint.GetComponentInChildren<SpriteRenderer>(true);

        // Hide by default; StrokeGuide.Show() decides when to play
        if (hint) hint.gameObject.SetActive(false);
    }

    void Start()
    {
        // Optional auto-play (generally leave off, StrokeGuide controls this)
        if (playOnStart) Show();
    }

    // Public API used by StrokeGuide
    public void Show()
    {
        BuildPath();

        if (pathWorld == null || pathWorld.Length < 2)
        {
            Debug.LogWarning("[IdleTraceHint] No valid path to play.", this);
            return;
        }

        // Starting pose
        int startIndex = (startAnchor == StartAnchor.Start) ? 0 : pathWorld.Length - 1;
        hint.position = pathWorld[startIndex];

        if (alignRotationToPath)
            AlignRotationAt(startIndex);
        else
            hint.rotation = Quaternion.Euler(0f, 0f, fixedZRotation);

        if (hintRenderer) SetAlpha(1f);
        hint.gameObject.SetActive(true);

        if (runRoutine != null) StopCoroutine(runRoutine);
        runRoutine = StartCoroutine(RunFollow());
    }

    public void Hide()
    {
        if (runRoutine != null)
        {
            StopCoroutine(runRoutine);
            runRoutine = null;
        }
        if (hint) hint.gameObject.SetActive(false);
    }

    public void Restart()
    {
        Hide();
        Show();
    }

    void OnDisable()
    {
        if (runRoutine != null) StopCoroutine(runRoutine);
        runRoutine = null;
    }

    // Build a world-space path from either child transforms or edge points
    private void BuildPath()
    {
        if (pathSource == PathSource.WaypointTransforms)
        {
            if (!waypointsRoot || waypointsRoot.childCount < 2)
            {
                pathWorld = null;
                return;
            }

            int n = waypointsRoot.childCount;
            pathWorld = new Vector3[n + (closeLoop ? 1 : 0)];
            for (int i = 0; i < n; i++)
                pathWorld[i] = waypointsRoot.GetChild(i).position;

            if (closeLoop) pathWorld[n] = pathWorld[0];

            // Apply perpendicular offset per segment (approximate first segment)
            ApplyOffsetApprox();
        }
        else // EdgeColliderPoints
        {
            if (!edge || edge.pointCount < 2)
            {
                pathWorld = null;
                return;
            }

            var pts = edge.points; // local to edge.transform
            int n = pts.Length;
            int total = n + (closeLoop ? 1 : 0);
            pathWorld = new Vector3[total];
            for (int i = 0; i < n; i++)
                pathWorld[i] = edge.transform.TransformPoint(pts[i]);
            if (closeLoop) pathWorld[n] = pathWorld[0];

            ApplyOffsetPerSegment();
        }
    }

    private IEnumerator RunFollow()
    {
        // Fade in
        yield return FadeTo(1f, fadeDuration);

        int dir = (startAnchor == StartAnchor.Start) ? +1 : -1;
        int index = (startAnchor == StartAnchor.Start) ? 0 : pathWorld.Length - 1;

        while (true)
        {
            int nextIndex = index + dir;

            // End handling
            if (nextIndex < 0 || nextIndex >= pathWorld.Length)
            {
                // Reached the end
                yield return FadeTo(0f, fadeDuration);
                yield return new WaitForSeconds(pauseDuration);

                if (loopMode == LoopMode.Once)
                {
                    // Stop after one pass
                    hint.gameObject.SetActive(false);
                    runRoutine = null;
                    yield break;
                }
                else if (loopMode == LoopMode.Loop)
                {
                    // restart same direction
                    index = (startAnchor == StartAnchor.Start) ? 0 : pathWorld.Length - 1;
                    if (hintRenderer) SetAlpha(1f);
                    hint.gameObject.SetActive(true);
                    continue;
                }
                else // PingPong
                {
                    dir *= -1;
                    // clamp just inside the path bounds
                    index = Mathf.Clamp(index + dir, 0, pathWorld.Length - 1);
                    if (hintRenderer) SetAlpha(1f);
                    hint.gameObject.SetActive(true);
                    continue;
                }
            }

            Vector3 from = pathWorld[index];
            Vector3 to = pathWorld[nextIndex];

            float len = Vector3.Distance(from, to);
            float duration = Mathf.Max(0.0001f, len / hintSpeed);
            float t = 0f;

            // Rotation handling
            if (alignRotationToPath)
                hint.rotation = Quaternion.LookRotation(Vector3.forward, (to - from).normalized);
            else
                hint.rotation = Quaternion.Euler(0f, 0f, fixedZRotation);

            // Move
            while (t < 1f)
            {
                hint.position = Vector3.Lerp(from, to, t);
                t += Time.deltaTime / duration;
                yield return null;
            }
            hint.position = to;

            index = nextIndex;
        }
    }

    private void ApplyOffsetApprox()
    {
        if (Mathf.Approximately(offsetDistance, 0f)) return;
        if (pathWorld == null || pathWorld.Length < 2) return;

        // Use first segment to define offset direction (simple, consistent)
        Vector3 a = pathWorld[0];
        Vector3 b = pathWorld[1];
        Vector3 dir = (b - a).normalized;
        Vector3 perp = new Vector3(-dir.y, dir.x, 0f) * offsetDistance;

        for (int i = 0; i < pathWorld.Length; i++)
            pathWorld[i] += perp;
    }

    private void ApplyOffsetPerSegment()
    {
        if (Mathf.Approximately(offsetDistance, 0f)) return;
        if (pathWorld == null || pathWorld.Length < 2) return;

        // Per-point offset using adjacent segment (smoother around curves)
        Vector3[] offset = new Vector3[pathWorld.Length];

        for (int i = 0; i < pathWorld.Length - 1; i++)
        {
            Vector3 dir = (pathWorld[i + 1] - pathWorld[i]).normalized;
            Vector3 perp = new Vector3(-dir.y, dir.x, 0f) * offsetDistance;
            offset[i] += perp;
            offset[i + 1] += perp;
        }

        for (int i = 0; i < pathWorld.Length; i++)
            pathWorld[i] += offset[i] * 0.5f; // average
    }

    private void AlignRotationAt(int idx)
    {
        int next = Mathf.Clamp(idx + 1, 0, pathWorld.Length - 1);
        Vector3 dir = (pathWorld[next] - pathWorld[idx]).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f; // up axis
        hint.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (!hintRenderer || duration <= 0f)
            yield break;

        float startAlpha = hintRenderer.color.a;
        float t = 0f;
        while (t < 1f)
        {
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            t += Time.deltaTime / duration;
            yield return null;
        }
        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float a)
    {
        if (!hintRenderer) return;
        var c = hintRenderer.color;
        c.a = a;
        hintRenderer.color = c;
    }
}
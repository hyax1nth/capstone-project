using UnityEngine;
using System.Collections;

public class IdleTraceHint : MonoBehaviour
{
    [Header("Hint Settings")]
    public Transform hint;                 // The hand sprite
    public Transform waypointsRoot;        // Root container of waypoints
    public float hintSpeed = 2.0f;         // Glide speed
    public float fadeDuration = 0.5f;      // Fade in/out time
    public float pauseDuration = 1f;       // Pause before repeating
    public float fixedZRotation = 50f;     // Consistent rotation

    private Transform[] waypoints;
    private int currentIndex = 0;
    private bool isFading = false;
    private SpriteRenderer hintRenderer;

    void Start()
    {
        // Cache waypoints
        int count = waypointsRoot.childCount;
        waypoints = new Transform[count];
        for (int i = 0; i < count; i++)
        {
            waypoints[i] = waypointsRoot.GetChild(i);
        }

        // Get renderer for fading
        hintRenderer = hint.GetComponent<SpriteRenderer>();
        hint.position = waypoints[0].position;
        hint.rotation = Quaternion.Euler(0f, 0f, fixedZRotation);
        hintRenderer.color = new Color(1f, 1f, 1f, 1f); // fully visible
    }

    public void Show()
    {
        if (hint != null)
            hint.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (hint != null)
            hint.gameObject.SetActive(false);
    }

    void Update()
    {
        if (isFading || waypoints == null || waypoints.Length == 0) return;

        Transform target = waypoints[currentIndex];
        hint.position = Vector3.MoveTowards(hint.position, target.position, hintSpeed * Time.deltaTime);
        hint.rotation = Quaternion.Euler(0f, 0f, fixedZRotation);

        if (Vector3.Distance(hint.position, target.position) < 0.01f)
        {
            currentIndex++;
            if (currentIndex >= waypoints.Length)
            {
                StartCoroutine(FadeOutResetFadeIn());
            }
        }
    }

    IEnumerator FadeOutResetFadeIn()
    {
        isFading = true;

        // Fade out
        yield return FadeTo(0f);

        // Reset position
        currentIndex = 0;
        hint.position = waypoints[0].position;

        // Optional pause
        yield return new WaitForSeconds(pauseDuration);

        // Fade in
        yield return FadeTo(1f);

        isFading = false;
    }

    IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = hintRenderer.color.a;
        float t = 0f;
        while (t < fadeDuration)
        {
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t / fadeDuration);
            hintRenderer.color = new Color(1f, 1f, 1f, alpha);
            t += Time.deltaTime;
            yield return null;
        }
        hintRenderer.color = new Color(1f, 1f, 1f, targetAlpha);
    }
}
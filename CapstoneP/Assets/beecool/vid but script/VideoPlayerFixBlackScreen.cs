using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoPlayerFixBlackScreen : MonoBehaviour
{
    [Header("Video Settings")]
    public int width = 848;
    public int height = 480;
    public RawImage rawImage;
    public float fadeInDuration = 0.5f; // Duration of fade in animation

    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        
        // Create or get CanvasGroup for fading
        canvasGroup = rawImage.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = rawImage.gameObject.AddComponent<CanvasGroup>();
        
        // Start hidden
        canvasGroup.alpha = 0;

        // Create render texture with transparency
        renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        renderTexture.Create();

        // Set up video player
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        rawImage.texture = renderTexture;

        // Register prepare completed callback
        videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        // Start fade in when video is prepared
        StartCoroutine(FadeIn());
        
        // Ensure video loops smoothly
        videoPlayer.isLooping = true;
        videoPlayer.Play();
    }

    private IEnumerator FadeIn()
    {
        float elapsedTime = 0;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = elapsedTime / fadeInDuration;
            yield return null;
        }
        canvasGroup.alpha = 1;
    }

    private void OnDestroy()
    {
        // Clean up
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }
    }
}

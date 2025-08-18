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

        // Validate inputs
        if (rawImage == null)
        {
            Debug.LogError("VideoPlayerFixBlackScreen: rawImage is not assigned.");
            return;
        }

        if (width <= 0 || height <= 0)
        {
            Debug.LogWarning($"VideoPlayerFixBlackScreen: invalid width/height ({width}x{height}), falling back to 640x360.");
            width = 640; height = 360;
        }

        // Create render texture with transparency - defensive creation to avoid D3D12 shared-handle failures
        try
        {
            renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            renderTexture.useMipMap = false;
            renderTexture.autoGenerateMips = false;
            renderTexture.Create();

            // Set up video player render mode; defer assigning targetTexture until the player is prepared
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            // Register prepare completed callback
            videoPlayer.prepareCompleted += OnVideoPrepared;
            try
            {
                // Prepare the video; assignment of targetTexture and Play will happen in OnVideoPrepared
                videoPlayer.Prepare();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"VideoPlayerFixBlackScreen: Prepare() threw: {ex.Message}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"VideoPlayerFixBlackScreen: failed creating RenderTexture or assigning it: {ex.Message}");
            // Ensure we don't leave a dangling assignment
            if (videoPlayer != null) videoPlayer.targetTexture = null;
            if (renderTexture != null)
            {
                try { renderTexture.Release(); } catch { }
                try { Destroy(renderTexture); } catch { }
                renderTexture = null;
            }
        }
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        // Assign the renderTexture to the video player now that it's prepared.
        try
        {
            if (videoPlayer != null) videoPlayer.targetTexture = renderTexture;
            if (rawImage != null) rawImage.texture = renderTexture;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"VideoPlayerFixBlackScreen: failed assigning targetTexture on prepare: {ex.Message}");
        }

        // Start fade in when video is prepared
        StartCoroutine(FadeIn());

        // Ensure video loops smoothly and start playback
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
            // Clear the video player's target before releasing to avoid D3D shared-handle errors on Windows
            if (videoPlayer != null) videoPlayer.targetTexture = null;
            try { renderTexture.Release(); } catch { }
            try { Destroy(renderTexture); } catch { }
        }

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoCustomTexture : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public RawImage rawImage;
    public RectTransform _rect;

    void Start()
    {
        var renderTexture = new CustomRenderTexture((int)_rect.rect.width, (int)_rect.rect.height);
        renderTexture.initializationColor = new Color(0f, 0f, 0f, 0f); // Transparent
        renderTexture.initializationMode = CustomRenderTextureUpdateMode.OnLoad;
        renderTexture.Create();

        videoPlayer.targetTexture = renderTexture;
        rawImage.texture = renderTexture;

        videoPlayer.Play();
    }
}

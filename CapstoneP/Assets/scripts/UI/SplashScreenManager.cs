using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;

public class SplashScreenManager : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string nextSceneName = "MainMenu";  // The scene to load after splash

    void Start()
    {
        // Subscribe to video completion event
        videoPlayer.loopPointReached += OnVideoFinished;
        
        // If video fails to play, we don't want to get stuck
        StartCoroutine(EmergencyTimeOut());
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator EmergencyTimeOut()
    {
        // Wait a maximum of 5 seconds or video length + 1 second, whichever is longer
        float timeOut = Mathf.Max(5, (float)videoPlayer.length + 1f);
        yield return new WaitForSeconds(timeOut);
        
        // If we're still in the splash scene after timeout, force load next scene
        if (SceneManager.GetActiveScene().name == "SplashScreen")
        {
            LoadNextScene();
        }
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}

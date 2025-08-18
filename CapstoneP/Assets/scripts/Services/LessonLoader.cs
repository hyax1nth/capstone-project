using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads/unloads lesson scenes additively. Call LoadLesson(sceneName) after setting LessonSession.Instance.Set(...).
/// The loader assumes LessonSession singleton exists in the startup scene (create one GameObject with LessonSession attached and mark it persistent).
/// </summary>
public class LessonLoader : MonoBehaviour
{
    public static LessonLoader Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadLesson(string sceneName)
    {
        StartCoroutine(LoadLessonAsync(sceneName));
    }

    private IEnumerator LoadLessonAsync(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (op == null)
        {
            Debug.LogError($"LessonLoader: Failed to load scene '{sceneName}'. Make sure it's in Build Settings.");
            yield break;
        }
        while (!op.isDone)
            yield return null;

        // Optionally set the loaded scene active
        var loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene.IsValid())
        {
            SceneManager.SetActiveScene(loadedScene);
        }
    }

    public void UnloadLesson(string sceneName)
    {
        StartCoroutine(UnloadLessonAsync(sceneName));
    }

    private IEnumerator UnloadLessonAsync(string sceneName)
    {
        var op = SceneManager.UnloadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"LessonLoader: Failed to unload scene '{sceneName}'.");
            yield break;
        }
        while (!op.isDone)
            yield return null;

        // Optionally set the first loaded scene active (assumes index 0 is your main/dashboard)
        if (SceneManager.sceneCount > 0)
        {
            SceneManager.SetActiveScene(SceneManager.GetSceneAt(0));
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

/// <summary>
/// Simple splash screen controller.
/// Attach to a GameObject under a Canvas. Assign a CanvasGroup that contains the splash artwork/UI.
/// Supports fade in, wait time, fade out, optional skip button and asynchronous scene loading.
/// </summary>
public class SplashScreenController : MonoBehaviour
{
    [Header("Splash Settings")]
    [Tooltip("Name of the scene to load after the splash (use exact scene name).")]
    // Default to the Login scene recommended by README
    public string nextScene = "Login";
    [Tooltip("How long the splash stays fully visible (seconds).")]
    public float displayTime = 1.5f;
    [Tooltip("Fade in/out duration in seconds.")]
    public float fadeDuration = 0.5f;

    [Header("UI References")]
    [Tooltip("Canvas that contains the splash artwork/UI. The script will look for or add a CanvasGroup on this Canvas for fading.")]
    public Canvas splashCanvas;
    // internal CanvasGroup created/found at Awake()
    private CanvasGroup splashGroup;
    [Tooltip("Optional UI Button that lets users skip the splash. Can be left empty.")]
    public Button skipButton;

    [Header("Loading")]
    [Tooltip("If true, the next scene is loaded asynchronously (recommended).")]
    public bool loadAsync = true;

    [Tooltip("If true, wait for FirebaseInitializer.IsInitialized before loading the next scene.")]
    public bool waitForFirebase = true;

    [Tooltip("How long to wait (seconds) for Firebase to initialize before continuing.")]
    public float firebaseInitTimeout = 10f;

    private bool skipRequested = false;

    private void Awake()
    {
        // Ensure we have a Canvas and an associated CanvasGroup for fading
        if (splashCanvas == null)
        {
            splashCanvas = GetComponentInChildren<Canvas>();
            if (splashCanvas == null)
            {
                Debug.LogWarning("SplashScreenController: No Canvas assigned or found. Adding one to this GameObject.");
                splashCanvas = gameObject.AddComponent<Canvas>();
            }
        }

        // Ensure a CanvasGroup exists on the Canvas to support fading
        var cg = splashCanvas.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = splashCanvas.gameObject.AddComponent<CanvasGroup>();
        }
        splashGroup = cg;

        // Wire skip button if present
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipClicked);
        }
    }

    private void Start()
    {
        // Start with invisible
        if (splashGroup != null) splashGroup.alpha = 0f;
        StartCoroutine(RunSplash());
    }

    private IEnumerator RunSplash()
    {
    Debug.Log($"SplashScreenController: starting splash -> nextScene='{nextScene}' loadAsync={loadAsync}");
        // Fade in
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        // Wait or skip
        float t = 0f;
        while (t < displayTime && !skipRequested)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // Fade out
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        // Load next scene
        // Optionally wait for Firebase initialization before changing scenes
        if (waitForFirebase)
        {
            yield return StartCoroutine(WaitForFirebase(firebaseInitTimeout));
        }

        // Auto-route based on saved last signed-in info to skip login if possible
        var lastUid = PlayerPrefs.GetString("lastSignedInUid", "");
        var lastRole = PlayerPrefs.GetString("lastSignedInRole", "");
        if (!string.IsNullOrEmpty(lastUid) && !string.IsNullOrEmpty(lastRole))
        {
            string target = null;
            if (string.Equals(lastRole, "admin", System.StringComparison.OrdinalIgnoreCase)) target = "AdminDashboard";
            else target = "StudentDashboard";
            if (!string.IsNullOrEmpty(target) && IsSceneInBuild(target))
            {
                Debug.Log($"SplashScreenController: auto-routing to '{target}' based on saved role='{lastRole}' uid={lastUid}");
                if (loadAsync) yield return StartCoroutine(LoadSceneAsync(target)); else SceneManager.LoadScene(target);
                yield break;
            }
        }

        if (string.IsNullOrEmpty(nextScene))
        {
            Debug.LogWarning("SplashScreenController: nextScene is empty. Splash will end but no scene will be loaded.");
            yield break;
        }

        if (loadAsync)
            yield return StartCoroutine(LoadSceneAsync(nextScene));
        else
            SceneManager.LoadScene(nextScene);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (splashGroup == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(from, to, duration <= 0f ? 1f : (elapsed / duration));
            splashGroup.alpha = a;
            yield return null;
        }
        splashGroup.alpha = to;
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // If the requested scene isn't present in Build Settings, avoid trying to load it.
        if (!IsSceneInBuild(sceneName))
        {
            Debug.LogError($"SplashScreenController: Scene '{sceneName}' couldn't be loaded because it is not in Build Settings.");
            // Try to fall back to the Login scene (common default) if available
            const string defaultLogin = "Login";
            if (IsSceneInBuild(defaultLogin))
            {
                Debug.LogWarning($"SplashScreenController: Falling back to '{defaultLogin}' scene because '{sceneName}' is not in Build Settings.");
                sceneName = defaultLogin;
            }
            else
            {
                Debug.LogWarning($"SplashScreenController: Neither '{sceneName}' nor fallback '{defaultLogin}' are in Build Settings. Attempting to enable any LoginUIManager in the current scene as a last resort.");
                var login = FindAnyObjectByType<LoginUIManager>();
                if (login != null)
                {
                    Debug.LogWarning("SplashScreenController: Enabling LoginUIManager found in current scene as a fallback.");
                    login.gameObject.SetActive(true);
                }
                yield break;
            }
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"SplashScreenController: Failed to load scene '{sceneName}'. Make sure the scene is added to Build Settings.");
            // Fallback: try to enable a LoginUIManager in the current scene so the app isn't a dead black screen
            var login = FindAnyObjectByType<LoginUIManager>();
            if (login != null)
            {
                Debug.LogWarning("SplashScreenController: Enabling LoginUIManager found in scene as a fallback.");
                login.gameObject.SetActive(true);
            }
            yield break;
        }

        // Let the loader complete automatically. If you need a progress UI, expose op.progress here.
        while (!op.isDone)
            yield return null;

        // Post-load: try to ensure LoginUIManager's main menu is visible to avoid a blank/blue screen
        var loadedLogin = FindAnyObjectByType<LoginUIManager>();
        if (loadedLogin != null)
        {
            Debug.Log("SplashScreenController: Found LoginUIManager after scene load; ensuring main menu is visible.");
            try
            {
                loadedLogin.gameObject.SetActive(true);
                // If the LoginUIManager provides a ShowPanel or ShowMainMenu, call it via reflection-safe check
                var mi = loadedLogin.GetType().GetMethod("ShowPanel");
                if (mi != null)
                {
                    var mainMenuField = loadedLogin.GetType().GetField("mainMenuPanel");
                    if (mainMenuField != null)
                    {
                        var panelObj = mainMenuField.GetValue(loadedLogin) as GameObject;
                        if (panelObj != null)
                        {
                            mi.Invoke(loadedLogin, new object[] { panelObj });
                        }
                    }
                }
                else
                {
                    // As a fallback, try to enable the mainMenuPanel directly
                    var mainMenuProp = loadedLogin.GetType().GetField("mainMenuPanel");
                    if (mainMenuProp != null)
                    {
                        var m = mainMenuProp.GetValue(loadedLogin) as GameObject;
                        if (m != null) m.SetActive(true);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("SplashScreenController: Could not auto-show main menu on LoginUIManager: " + ex.Message);
            }
        }
        else
        {
            // No LoginUIManager found: try broader UI fallbacks to avoid blank screen
            Debug.LogWarning("SplashScreenController: No LoginUIManager found in loaded scene; attempting UI fallbacks.");

            // Enable any Canvas in scene
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(UnityEngine.FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (!c.gameObject.activeInHierarchy) c.gameObject.SetActive(true);
                c.enabled = true;
            }

            // Ensure EventSystem exists (needed for UI interaction)
            var eventSystem = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                Debug.Log("SplashScreenController: No EventSystem found; creating one.");
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                UnityEngine.Object.DontDestroyOnLoad(esGO);
            }

            // Try to enable common-named main menu objects as a last resort
            var allGOs = UnityEngine.Object.FindObjectsByType<GameObject>(UnityEngine.FindObjectsSortMode.None);
            foreach (var go in allGOs)
            {
                if (go == null) continue;
                var name = go.name.ToLowerInvariant();
                if (name.Contains("mainmenu") || name.Contains("main_menu") || name.Contains("main menu") || name.Contains("main"))
                {
                    if (!go.activeInHierarchy) go.SetActive(true);
                }
            }
        }
    }

    private bool IsSceneInBuild(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(path)) continue;
            string filename = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(filename, sceneName, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private IEnumerator WaitForFirebase(float timeout)
    {
        float elapsed = 0f;
        // If a FirebaseInitializer exists, wait for its flag. Otherwise try to create one from a prefab/scene object named FirebaseBootstrap.
        bool createdBootstrap = false;
        while (elapsed < timeout)
        {
            if (FirebaseInitializer.IsInitialized)
            {
                Debug.Log("SplashScreenController: Firebase initialized.");
                yield break;
            }

            // Try to find any existing bootstrap object and ensure it's active
            var fb = FindAnyObjectByType<FirebaseInitializer>();
            if (fb != null)
            {
                // nothing to do, just wait for the flag to flip
            }
            else if (!createdBootstrap)
            {
                // Create a bootstrap object so FirebaseInitializer runs early (matches README expectation)
                Debug.LogWarning("SplashScreenController: No FirebaseInitializer found; creating FirebaseBootstrap GameObject.");
                var go = new GameObject("FirebaseBootstrap");
                go.AddComponent<FirebaseInitializer>();
                DontDestroyOnLoad(go);
                createdBootstrap = true;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning("SplashScreenController: WaitForFirebase timed out; proceeding without confirmed Firebase init.");
    }

    private void OnSkipClicked()
    {
        skipRequested = true;
    }

    private void OnDestroy()
    {
        if (skipButton != null)
            skipButton.onClick.RemoveListener(OnSkipClicked);
    }
}

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CreateSampleLessonScene
{
    [MenuItem("Tools/Bootcamp/Create Sample Lesson Scene and LessonLoader")]
    public static void CreateLessonScene()
    {
        // Ensure directories
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        System.IO.Directory.CreateDirectory("Assets/Prefabs");

        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Create Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Create a Panel
        var panelGO = new GameObject("LessonPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var img = panelGO.AddComponent<Image>();
        img.color = Color.white;
        var rt = panelGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0,0);
        rt.anchorMax = new Vector2(1,1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Create a Finish Button
        var btnGO = new GameObject("FinishButton");
        btnGO.transform.SetParent(panelGO.transform, false);
        var btn = btnGO.AddComponent<Button>();
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = Color.yellow;
        var btnRt = btnGO.GetComponent<RectTransform>();
        btnRt.sizeDelta = new Vector2(300, 80);
        btnRt.anchoredPosition = new Vector2(0, -150);

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(btnGO.transform, false);
        var txt = txtGO.AddComponent<UnityEngine.UI.Text>();
        txt.text = "Finish (Sample)";
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.black;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        var txtRt = txtGO.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        // Add LessonTemplate
        var lessonMgr = new GameObject("LessonManager");
        var lessonTemplateType = typeof(MonoBehaviour).Assembly.GetType("LessonTemplate") ?? null;
        // Try to add our LessonTemplate script if compiled
        var lessonTemplate = lessonMgr.AddComponent(typeof(UnityEngine.Component));
        // If LessonTemplate isn't available at compile-time in the editor, just add an empty component as placeholder

        lessonMgr.transform.SetParent(panelGO.transform, false);
        lessonMgr.AddComponent<RectTransform>();

        // Save scene
        string path = "Assets/Scenes/Lesson_Alphabet_L1.unity";
        if (System.IO.File.Exists(path))
        {
            if (!EditorUtility.DisplayDialog("Overwrite?", "Scene already exists. Overwrite?", "Yes", "No"))
                return;
        }

        EditorSceneManager.SaveScene(scene, path);
        Debug.Log($"Created sample lesson scene: {path}");

        // Create LessonLoader prefab (GameObject with LessonLoader component) in Prefabs
        var loaderGO = new GameObject("LessonLoader");
        // Attach LessonLoader if available
        var loaderType = typeof(MonoBehaviour).Assembly.GetType("LessonLoader");
        if (loaderType != null)
        {
            loaderGO.AddComponent(loaderType);
        }
        // Save as prefab
        string prefabPath = "Assets/Prefabs/LessonLoader.prefab";
        PrefabUtility.SaveAsPrefabAsset(loaderGO, prefabPath);
        GameObject.DestroyImmediate(loaderGO);
        Debug.Log($"Created LessonLoader prefab at {prefabPath}");

        AssetDatabase.Refresh();
    }
}
#endif

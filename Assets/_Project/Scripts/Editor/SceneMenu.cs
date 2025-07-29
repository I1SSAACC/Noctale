using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEditor.SceneManagement;

public class SceneMenu : EditorWindow
{
    private string[] scenePaths;
    private double lastUpdateTime;

    [MenuItem("Tools/Scene Manager")]
    public static void ShowWindow() => GetWindow<SceneMenu>("Scene Manager");

    private void OnEnable()
    {
        LoadScenes();
        lastUpdateTime = EditorApplication.timeSinceStartup;
    }

    private void LoadScenes()
    {
        string scenesFolder = "Assets/_Project/Scenes";
        if (Directory.Exists(scenesFolder))
        {
            scenePaths = Directory.GetFiles(scenesFolder, "*.unity");
        }
        else
        {
            Debug.LogWarning($"Directory not found: {scenesFolder}");
            scenePaths = new string[0];
        }
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("Available Scenes", EditorStyles.boldLabel);

        if (scenePaths != null && scenePaths.Length > 0)
        {
            foreach (var scenePath in scenePaths)
            {
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                if (GUILayout.Button(sceneName))
                    OpenScene(scenePath);
            }
        }
        else
        {
            GUILayout.Label("No scenes found in Assets/Scenes");
        }
    }

    private void OpenScene(string scenePath)
    {
        if (EditorSceneManager.GetActiveScene().isDirty)
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        if (File.Exists(scenePath))
        {
            EditorSceneManager.OpenScene(scenePath);
            Debug.Log($"Opened scene: {Path.GetFileNameWithoutExtension(scenePath)}");
        }
        else
        {
            Debug.LogWarning($"Scene not found: {scenePath}");
        }
    }

    private void OnInspectorUpdate()
    {
        if (EditorApplication.timeSinceStartup - lastUpdateTime > 2)
        {
            LoadScenes();
            lastUpdateTime = EditorApplication.timeSinceStartup;
            Repaint();
        }
    }
}

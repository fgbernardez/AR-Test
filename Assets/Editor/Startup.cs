using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class ForceBoot
{
    static ForceBoot()
    {
        // This grabs whatever scene is #0 in your Build Settings
        var pathOfFirstScene = EditorBuildSettings.scenes[0].path;
        
        // This tells Unity: "Always start play mode with this scene"
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(pathOfFirstScene);
    }
}
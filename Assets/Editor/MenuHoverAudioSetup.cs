using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MenuHoverAudioSetup
{
    private const string ScenePath = "Assets/Scenes/Menu_Scene.unity";
    private const string HoverClipPath = "Assets/AudioClip/SFX_UI_Click_Organic_Plastic_Thin_Sharp_Generic_1.ogg";

    [MenuItem("Tools/OnlyOneUp/Setup Menu Hover Audio")]
    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>(HoverClipPath);
        if (hoverClip == null)
        {
            Debug.LogError("Khong tim thay hover audio clip tai: " + HoverClipPath);
            return;
        }

        // 1. Gan buttonHover vao UIButtonSFX
        var uiButtonSFX = Object.FindFirstObjectByType<UIButtonSFX>();
        if (uiButtonSFX != null)
        {
            Undo.RecordObject(uiButtonSFX, "Assign Button Hover Sound");
            uiButtonSFX.buttonHover = hoverClip;
            EditorUtility.SetDirty(uiButtonSFX);
            Debug.Log("Da gan buttonHover vao UIButtonSFX.");
        }

        // 2. Gan component UIButtonHoverSound vao 4 Button trong Main Menu
        string[] buttonNames = new string[] { "StartButton", "CreditButton", "Option", "ExitButton" };
        foreach (var btnName in buttonNames)
        {
            var go = GameObject.Find(btnName);
            if (go != null)
            {
                var hoverComp = go.GetComponent<UIButtonHoverSound>();
                if (hoverComp == null)
                {
                    hoverComp = Undo.AddComponent<UIButtonHoverSound>(go);
                }
                EditorUtility.SetDirty(go);
                Debug.Log($"Da gan UIButtonHoverSound cho button: {btnName}");
            }
            else
            {
                Debug.LogWarning($"Khong tim thay button: {btnName}");
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Setup Menu Hover Audio thanh cong va da luu Scene!");
    }
}

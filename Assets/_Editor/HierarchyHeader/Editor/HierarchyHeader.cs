using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class HierarchyHeader
{
    static HierarchyHeader()
    {
        EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
    }

    static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        var obj = EditorUtility.EntityIdToObject(instanceID) as GameObject;

        
        if (obj != null && obj.name.StartsWith("-"))
        {
            
            EditorGUI.DrawRect(selectionRect, new Color(0.2f, 0.2f, 0.2f, 1f));

           
            GUIStyle textStyle = new GUIStyle();
            textStyle.normal.textColor = Color.yellow; 
            textStyle.alignment = TextAnchor.MiddleCenter;
            textStyle.fontStyle = FontStyle.Bold;

            
            GUI.Label(selectionRect, obj.name.Replace("-", ""), textStyle);
        }
    }
}
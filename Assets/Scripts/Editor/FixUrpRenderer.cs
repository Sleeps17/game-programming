using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Создаёт 3D Universal Renderer и переключает на него URP-пайплайн (по умолчанию шаблон URP
// ставит 2D-рендерер, в котором 3D point/spot источники не освещают поверхности).
public static class FixUrpRenderer
{
    private const string PipelinePath  = "Assets/Settings/UniversalRP.asset";
    private const string Renderer3DPath = "Assets/Settings/UniversalRenderer.asset";

    [MenuItem("Umbra/Fix URP to 3D Renderer")]
    public static void FixWithDialog()
    {
        if (!Fix()) return;
        EditorUtility.DisplayDialog("Umbra",
            "URP now uses the 3D Universal Renderer.\n\n" +
            "Restart Play mode to see point/spot lights illuminate surfaces.",
            "OK");
    }

    public static bool Fix()
    {
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if (pipeline == null)
        {
            Debug.LogWarning($"[Umbra] Pipeline asset not found at {PipelinePath}");
            return false;
        }

        var renderer3D = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Renderer3DPath);
        if (renderer3D == null)
        {
            renderer3D = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer3D, Renderer3DPath);
            Debug.Log($"[Umbra] Created 3D Universal Renderer at {Renderer3DPath}");
        }

        var so = new SerializedObject(pipeline);
        var list = so.FindProperty("m_RendererDataList");
        list.ClearArray();
        list.InsertArrayElementAtIndex(0);
        list.GetArrayElementAtIndex(0).objectReferenceValue = renderer3D;
        so.FindProperty("m_DefaultRendererIndex").intValue = 0;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(pipeline);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Umbra] URP pipeline rewired to 3D Universal Renderer.");
        return true;
    }
}

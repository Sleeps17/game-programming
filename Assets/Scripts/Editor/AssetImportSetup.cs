using UnityEditor;
using UnityEngine;

// Переключает FBX-ассеты на Humanoid-риг и переимпортирует — без этого Mecanim не сможет
// перенацеливать анимации между разными моделями.
public static class AssetImportSetup
{
    [MenuItem("Umbra/Configure Imported Assets")]
    public static void ConfigureAllWithDialog()
    {
        var result = ConfigureAll();
        EditorUtility.DisplayDialog("Umbra — Asset Setup",
            $"Done!\n\n" +
            $"• Characters set to Humanoid: {result.chars}\n" +
            $"• Animations set to Humanoid: {result.anims}\n\n" +
            "Next: rebuild a level via Umbra menu — code will use these models.",
            "OK");
    }

    public static (int chars, int anims) ConfigureAll()
    {
        int fixedChars = ConfigureModelsAsHumanoid("Assets/Art/Characters");
        int fixedAnims = ConfigureModelsAsHumanoid("Assets/Art/Animations");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Umbra] Asset import: characters={fixedChars}, animations={fixedAnims}");
        return (fixedChars, fixedAnims);
    }

    static int ConfigureModelsAsHumanoid(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogWarning($"[Umbra] Folder not found: {folder}");
            return 0;
        }

        int count = 0;
        var guids = AssetDatabase.FindAssets("t:Model", new[] { folder });

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.ToLower().EndsWith(".fbx")) continue;

            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            bool changed = false;

            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }

            // Свой humanoid-аватар на основе этой модели — Mixamo-анимации тогда чисто перенацеливаются.
            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
                Debug.Log($"[Umbra] Configured as Humanoid: {path}");
                count++;
            }
        }

        return count;
    }
}

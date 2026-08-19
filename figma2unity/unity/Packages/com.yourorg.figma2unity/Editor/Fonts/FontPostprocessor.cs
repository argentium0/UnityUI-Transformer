using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace Figma2Unity.Editor.Fonts
{
    public class FontPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                if (string.IsNullOrEmpty(path)) continue;

                string ext = Path.GetExtension(path).ToLowerInvariant();
                if ((ext == ".ttf" || ext == ".otf") && path.Contains("Figma2UnityImports/Fonts"))
                {
                    Debug.Log($"[Figma2Unity FontPipeline] AssetPostprocessor triggered for newly imported font file: '{path}'");

                    // Force synchronous import to ensure the Font object is initialized
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                    Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(path);

                    if (sourceFont != null)
                    {
                        var bakedAsset = FontAssetBaker.BakeFontAsset(sourceFont, path);
                        if (bakedAsset != null)
                        {
                            Debug.Log($"[Figma2Unity FontPipeline] AssetPostprocessor successfully baked FontAsset for '{path}'");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[Figma2Unity FontPipeline] AssetPostprocessor loaded null Font object for '{path}'");
                    }
                }
            }
        }
    }
}
#endif

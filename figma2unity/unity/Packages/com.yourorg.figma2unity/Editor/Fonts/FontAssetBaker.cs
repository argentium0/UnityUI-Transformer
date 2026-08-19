using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Figma2Unity.Editor.Fonts
{
    public static class FontAssetBaker
    {
        public static UnityEngine.Object BakeFontAsset(Font sourceFont, string sourcePath, string targetFontsFolder = null)
        {
            if (sourceFont == null)
            {
                Debug.LogError($"[Figma2Unity FontPipeline] Cannot bake FontAsset: sourceFont is null for path '{sourcePath}'!");
                return null;
            }

#if UNITY_EDITOR
            try
            {
                Debug.Log($"[Figma2Unity FontPipeline] Triggering FontAsset creation for '{sourceFont.name}' from path '{sourcePath}'...");

                var tmpType = Type.GetType("TMPro.TMP_FontAsset, Unity.TextMeshPro") ?? Type.GetType("TMPro.TMP_FontAsset");
                if (tmpType != null)
                {
                    var createMethod = tmpType.GetMethod("CreateFontAsset", new Type[] { typeof(Font) });
                    if (createMethod != null)
                    {
                        var createdAsset = createMethod.Invoke(null, new object[] { sourceFont }) as UnityEngine.Object;
                        if (createdAsset != null)
                        {
                            if (string.IsNullOrEmpty(targetFontsFolder))
                            {
                                targetFontsFolder = Path.Combine("Assets", "Figma2Unity", "Generated", "Fonts");
                            }

                            if (!Directory.Exists(targetFontsFolder))
                            {
                                Directory.CreateDirectory(targetFontsFolder);
                            }

                            string fontName = Path.GetFileNameWithoutExtension(sourcePath);
                            string savePath = Path.Combine(targetFontsFolder, $"{fontName} SDF.asset").Replace('\\', '/');

                            AssetDatabase.CreateAsset(createdAsset, savePath);
                            AssetDatabase.SaveAssets();
                            AssetDatabase.ImportAsset(savePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                            Debug.Log($"[Figma2Unity FontPipeline] FontAsset successfully baked and saved at '{savePath}'");
                            return createdAsset;
                        }
                        else
                        {
                            Debug.LogError($"[Figma2Unity FontPipeline] TMP_FontAsset.CreateFontAsset returned null for '{sourceFont.name}'!");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[Figma2Unity FontPipeline] Could not find CreateFontAsset method on TMPro.TMP_FontAsset!");
                    }
                }
                else
                {
                    Debug.LogWarning($"[Figma2Unity FontPipeline] TMPro assembly not found. Skipping TMP_FontAsset baking.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Figma2Unity FontPipeline] Exception during FontAsset baking for '{sourceFont.name}': {ex.Message}\n{ex.StackTrace}");
            }
#endif
            return null;
        }
    }
}

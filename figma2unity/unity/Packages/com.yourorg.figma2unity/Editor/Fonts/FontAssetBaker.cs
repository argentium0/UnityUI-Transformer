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
            if (sourceFont == null) return null;

#if UNITY_EDITOR
            try
            {
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
                            Debug.Log($"[FontAssetBaker] Successfully baked TMP Font Asset for '{sourceFont.name}' at {savePath}");
                            return createdAsset;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FontAssetBaker] Failed to bake TMP Font Asset for {sourceFont.name}: {ex.Message}");
            }
#endif
            return null;
        }
    }
}

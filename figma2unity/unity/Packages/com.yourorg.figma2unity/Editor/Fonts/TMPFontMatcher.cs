using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Figma2Unity.Editor.Fonts
{
    public static class TMPFontMatcher
    {
        public class FontMatchResult
        {
            public bool Success;
            public bool WasGenerated;
            public bool UsedFallback;
            public UnityEngine.Object FontAsset;
            public string AssetPath;
            public string LogMessage;
        }

        public static FontMatchResult MatchOrGenerateFont(string fontFamily, float? fontWeight = 400f)
        {
            var result = new FontMatchResult();

            if (string.IsNullOrEmpty(fontFamily))
            {
                fontFamily = "Liberation Sans";
            }

            string weightSuffix = GetWeightSuffix(fontWeight ?? 400f);
            string targetName = $"{fontFamily}-{weightSuffix}";
            string simpleTargetName = fontFamily.Replace(" ", "");

#if UNITY_EDITOR
            // 1. Search for existing TMP Font Asset by asset name or title
            string[] tmpGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            foreach (string guid in tmpGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName.Equals(targetName, StringComparison.OrdinalIgnoreCase) ||
                    fileName.Equals(fontFamily, StringComparison.OrdinalIgnoreCase) ||
                    fileName.Replace(" ", "").Equals(simpleTargetName, StringComparison.OrdinalIgnoreCase))
                {
                    result.Success = true;
                    result.FontAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    result.AssetPath = path;
                    result.LogMessage = $"Matched existing TMP Font Asset: '{fileName}' at {path}";
                    return result;
                }
            }

            // 2. If missing, search project for raw .ttf / .otf Font File
            string[] fontGuids = AssetDatabase.FindAssets("t:Font");
            string rawFontPath = null;
            foreach (string guid in fontGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName.Equals(targetName, StringComparison.OrdinalIgnoreCase) ||
                    fileName.Equals(fontFamily, StringComparison.OrdinalIgnoreCase) ||
                    fileName.Replace(" ", "").Equals(simpleTargetName, StringComparison.OrdinalIgnoreCase))
                {
                    rawFontPath = path;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(rawFontPath))
            {
                Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(rawFontPath);
                if (sourceFont != null)
                {
                    var fontAsset = TryCreateTMPFontAsset(sourceFont, rawFontPath);
                    if (fontAsset != null)
                    {
                        result.Success = true;
                        result.WasGenerated = true;
                        result.FontAsset = fontAsset;
                        result.AssetPath = AssetDatabase.GetAssetPath(fontAsset);
                        result.LogMessage = $"Generated new TMP Font Asset for font '{fontFamily}' from {rawFontPath}";
                        return result;
                    }
                }
            }

            // 3. Fallback to default TMP Font Asset if no raw font file exists
            foreach (string guid in tmpGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("LiberationSans") || path.Contains("Default"))
                {
                    result.Success = true;
                    result.UsedFallback = true;
                    result.FontAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    result.AssetPath = path;
                    result.LogMessage = $"[Figma2Unity] Missing Font: Could not find or generate TMP Font Asset for '{fontFamily}' (Weight: {fontWeight}). Substituted default fallback: '{path}'.";
                    Debug.LogWarning(result.LogMessage);
                    return result;
                }
            }

            // Fallback log if no TMP asset found at all
            result.Success = false;
            result.UsedFallback = true;
            result.LogMessage = $"[Figma2Unity] Missing Font: No TMP Font Asset or raw font file found in project for '{fontFamily}' (Weight: {fontWeight}).";
            Debug.LogWarning(result.LogMessage);
#endif

            return result;
        }

#if UNITY_EDITOR
        private static UnityEngine.Object TryCreateTMPFontAsset(Font sourceFont, string sourcePath)
        {
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
                            string dir = Path.GetDirectoryName(sourcePath);
                            string savePath = Path.Combine(dir, $"{Path.GetFileNameWithoutExtension(sourcePath)} SDF.asset").Replace('\\', '/');
                            AssetDatabase.CreateAsset(createdAsset, savePath);
                            AssetDatabase.SaveAssets();
                            return createdAsset;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Figma2Unity] Failed to generate TMP Font Asset for {sourceFont.name}: {ex.Message}");
            }
            return null;
        }
#endif

        private static string GetWeightSuffix(float weight)
        {
            if (weight >= 700) return "Bold";
            if (weight >= 600) return "SemiBold";
            if (weight >= 500) return "Medium";
            if (weight <= 300) return "Light";
            return "Regular";
        }
    }
}

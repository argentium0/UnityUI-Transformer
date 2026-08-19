using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Figma2Unity.Editor.Schema;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Figma2Unity.Editor.Fonts
{
    public static class FontResolver
    {
        [Serializable]
        public class MissingFontReportEntry
        {
            public string FontFamily;
            public float FontWeight;
            public string NodeName;
            public string NodeId;
        }

        public class FontResolutionResult
        {
            public bool Success;
            public bool WasGenerated;
            public bool UsedFallback;
            public UnityEngine.Object FontAsset;
            public string AssetPath;
            public string LogMessage;
        }

        public static List<MissingFontReportEntry> MissingFontsReport { get; private set; } = new List<MissingFontReportEntry>();

        public static void ClearReport()
        {
            MissingFontsReport.Clear();
        }

        public static FontResolutionResult ResolveFontForTextNode(TextNode textNode, string fontsFolder = null)
        {
            if (textNode == null)
            {
                return new FontResolutionResult { Success = false };
            }

            string fontFamily = textNode.fontFamily;
            float fontWeight = textNode.fontWeight ?? 400f;
            string nodeName = textNode.name ?? "Text";
            string nodeId = textNode.id ?? "Unknown";

            return ResolveFont(fontFamily, fontWeight, nodeName, nodeId, fontsFolder);
        }

        public static FontResolutionResult ResolveFont(string fontFamily, float fontWeight, string nodeName, string nodeId, string fontsFolder = null)
        {
            var result = new FontResolutionResult();

            if (string.IsNullOrEmpty(fontFamily))
            {
                fontFamily = "Liberation Sans";
            }

            string weightSuffix = GetWeightSuffix(fontWeight);
            string targetName = $"{fontFamily}-{weightSuffix}";
            string simpleTargetName = fontFamily.Replace(" ", "");

#if UNITY_EDITOR
            // 1. Search project for an existing TMP Font Asset matching the family name
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

            // 2. If no TMP Font Asset exists, search for a raw .ttf / .otf file matching font family
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
                    var fontAsset = CreateAndSaveTMPFontAsset(sourceFont, rawFontPath, fontsFolder);
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

            // 3. If no raw font file exists locally, attempt to query Google Fonts API and download the TTF asset
            string downloadedTtfPath = GoogleFontFetcher.FetchGoogleFont(fontFamily);
            if (!string.IsNullOrEmpty(downloadedTtfPath))
            {
                Debug.Log($"[Figma2Unity FontPipeline] Synchronously importing downloaded TTF font at '{downloadedTtfPath}' into AssetDatabase...");
                AssetDatabase.ImportAsset(downloadedTtfPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                Font downloadedFont = AssetDatabase.LoadAssetAtPath<Font>(downloadedTtfPath);
                if (downloadedFont != null)
                {
                    Debug.Log($"[Figma2Unity FontPipeline] Successfully loaded Font asset from '{downloadedTtfPath}'. Baking FontAsset...");
                    var bakedAsset = FontAssetBaker.BakeFontAsset(downloadedFont, downloadedTtfPath, fontsFolder);

                    result.Success = true;
                    result.WasGenerated = true;
                    result.FontAsset = downloadedFont;
                    result.AssetPath = downloadedTtfPath;
                    result.LogMessage = $"Successfully downloaded and baked Google Font '{fontFamily}' at '{downloadedTtfPath}'";
                    return result;
                }
                else
                {
                    Debug.LogError($"[Figma2Unity FontPipeline] AssetDatabase.LoadAssetAtPath<Font> returned null for '{downloadedTtfPath}' despite synchronous import!");
                }
            }

            // 4. If no raw font file exists or fetch fails, log structured missing font warning and return fallback result
            string structuredWarning = $"[Figma2Unity FontPipeline] ERROR: Unable to resolve missing font: '{fontFamily}' ({fontWeight}) on node '{nodeName}' (ID: {nodeId})";
            Debug.LogError(structuredWarning);

            MissingFontsReport.Add(new MissingFontReportEntry
            {
                FontFamily = fontFamily,
                FontWeight = fontWeight,
                NodeName = nodeName,
                NodeId = nodeId
            });

            Figma2Unity.Editor.Reporting.FigmaImportLogger.LogMissingFont(fontFamily, fontWeight, nodeName, nodeId, "System Default");

            result.Success = true;
            result.UsedFallback = true;
            result.FontAsset = null;
            result.AssetPath = null;
            result.LogMessage = structuredWarning;
#endif

            return result;
        }

#if UNITY_EDITOR
        private static UnityEngine.Object CreateAndSaveTMPFontAsset(Font sourceFont, string sourcePath, string targetFontsFolder)
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
                            if (string.IsNullOrEmpty(targetFontsFolder))
                            {
                                targetFontsFolder = Path.Combine("Assets", "Figma2Unity", "Generated", "Fonts");
                            }

                            if (!Directory.Exists(targetFontsFolder))
                            {
                                Directory.CreateDirectory(targetFontsFolder);
                            }

                            string savePath = Path.Combine(targetFontsFolder, $"{Path.GetFileNameWithoutExtension(sourcePath)} SDF.asset").Replace('\\', '/');
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

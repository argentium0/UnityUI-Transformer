using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using Figma2Unity.Editor.Schema;
using Figma2Unity.Editor.Importer;

namespace Figma2Unity.Editor.Generator
{
    public static class UIToolkitGenerator
    {
        public class GenerationResult
        {
            public bool Success;
            public string USSPath;
            public List<string> UXMLPaths = new List<string>();
        }

        public static void RegenerateStylesheet(string stagingFolderPath)
        {
            string irJsonPath = Path.Combine(stagingFolderPath, "ir-document.json");
            if (!File.Exists(irJsonPath))
            {
                Debug.LogWarning($"[UIToolkitGenerator] No ir-document.json found at {stagingFolderPath}");
                return;
            }

            try
            {
                // Force UTF8 Encoding to prevent BOM corruption
                string jsonContent = File.ReadAllText(irJsonPath, System.Text.Encoding.UTF8);
                var settings = new Newtonsoft.Json.JsonSerializerSettings();
                settings.Converters.Add(new Figma2Unity.Editor.Schema.IRNodeConverter());
                var document = Newtonsoft.Json.JsonConvert.DeserializeObject<IRDocument>(jsonContent, settings);

                if (document != null)
                {
                    string packageName = Path.GetFileName(stagingFolderPath);
                    if (string.IsNullOrEmpty(packageName) || packageName == "Temp" || packageName == "Figma2UnitySync")
                    {
                        packageName = "LiveSyncPackage";
                    }

                    // Move downloaded images/vectors into the AssetDatabase
                    string stagingExports = Path.Combine(stagingFolderPath, "exports");
                    // We copy directly to Assets/Figma2UnityImports/packageName to match the USS path expectation
                    string destImports = Path.Combine("Assets", "Figma2UnityImports", packageName);
                    
                    if (Directory.Exists(stagingExports))
                    {
                        if (!Directory.Exists(destImports))
                        {
                            Directory.CreateDirectory(destImports);
                        }

                        // Copy all files recursively
                        foreach (string file in Directory.GetFiles(stagingExports, "*.*", SearchOption.AllDirectories))
                        {
                            string relative = file.Substring(stagingExports.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                            string sanitizedRelative = SanitizeAssetPath(relative);
                            string destFile = Path.Combine(destImports, sanitizedRelative);
                            string destDir = Path.GetDirectoryName(destFile);
                            if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                            File.Copy(file, destFile, true);
                        }

                        // Force Unity to register the new images BEFORE we generate the USS files
#if UNITY_EDITOR
                        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                        
                        // Enforce TextureImporter settings so UI Toolkit successfully reads them
                        string[] imageFiles = Directory.GetFiles(destImports, "*.*", SearchOption.AllDirectories);
                        foreach (string imgFile in imageFiles)
                        {
                            string ext = Path.GetExtension(imgFile).ToLowerInvariant();
                            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg") continue;

                            string assetPath = imgFile.Replace('\\', '/');
                            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                            if (importer != null)
                            {
                                importer.textureType = TextureImporterType.Sprite;
                                importer.spriteImportMode = SpriteImportMode.Single;
                                importer.alphaIsTransparency = true;
                                importer.SaveAndReimport();
                                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                            }
                        }
#endif
                    }

                    string destFolder = Path.Combine("Assets", "Figma2Unity", "Generated", packageName);
                    string fontsFolder = Path.Combine("Assets", "Figma2Unity", "Generated", "Fonts");
                    
                    // Pre-fetch missing fonts from Google Fonts and bake assets before USS generation
                    Figma2Unity.Editor.Importer.SyncPackageImporter.ResolveTextNodeFonts(document, fontsFolder);

                    Generate(document, destFolder, packageName);
                    Debug.Log($"[UIToolkitGenerator] Successfully regenerated stylesheet from {stagingFolderPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIToolkitGenerator] Failed to regenerate stylesheet: {ex.Message}");
            }
        }


        public static GenerationResult Generate(IRDocument document, string destinationFolder, string packageName)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (string.IsNullOrEmpty(destinationFolder))
            {
                destinationFolder = Path.Combine("Assets", "Figma2Unity", "Generated", packageName ?? "SyncPackage");
            }

            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            var result = new GenerationResult();
            var utf8Encoding = new System.Text.UTF8Encoding(false);

            // 1. Generate single shared USS Stylesheet for the package
            string ussContent = GenerateUSS(document, packageName);
            string ussFileName = $"{packageName}.uss";
            string ussFullPath = Path.Combine(destinationFolder, ussFileName);

            // Write in-place without File.Delete to preserve Unity UI Builder meta GUIDs
            File.WriteAllText(ussFullPath, ussContent, utf8Encoding);
            string ussUnityPath = ussFullPath.Replace('\\', '/');
            result.USSPath = ussUnityPath;

#if UNITY_EDITOR
            AssetDatabase.ImportAsset(ussUnityPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
#endif

            // 2. Generate one UXML file per top-level root IR node
            for (int i = 0; i < document.rootNodes.Count; i++)
            {
                var rootNode = document.rootNodes[i];
                string screenName = USSStyleGenerator.SanitizeClassName(rootNode.name ?? $"Screen_{i}", rootNode.id);
                string uxmlFileName = $"{screenName}.uxml";
                string uxmlFullPath = Path.Combine(destinationFolder, uxmlFileName);

                string relativeUssPath = ussFileName;
                string uxmlContent = UXMLTreeGenerator.GenerateUXML(rootNode, relativeUssPath);

                // Omit XML declaration if present so Unity's UI Builder parses the UXML file cleanly without utf-16 errors
                if (!string.IsNullOrEmpty(uxmlContent) && uxmlContent.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
                {
                    int endDecl = uxmlContent.IndexOf("?>", StringComparison.Ordinal);
                    if (endDecl >= 0)
                    {
                        uxmlContent = uxmlContent.Substring(endDecl + 2).TrimStart('\r', '\n');
                    }
                }

                // Write in-place without File.Delete to preserve Unity UI Builder meta GUIDs
                File.WriteAllText(uxmlFullPath, uxmlContent, utf8Encoding);
                string uxmlUnityPath = uxmlFullPath.Replace('\\', '/');
                result.UXMLPaths.Add(uxmlUnityPath);

#if UNITY_EDITOR
                AssetDatabase.ImportAsset(uxmlUnityPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
#endif
            }

            result.Success = true;

#if UNITY_EDITOR
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
#endif

            return result;
        }

        public static string GenerateUSS(IRDocument document, string packageName)
        {
            return USSStyleGenerator.GenerateUSS(document, packageName);
        }

        public static string SanitizeAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            string dir = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(fileName)) return path.Replace('\\', '/');

            string sanitizedFileName = Regex.Replace(fileName, @"[@\s]", "_");
            sanitizedFileName = Regex.Replace(sanitizedFileName, @"[^a-zA-Z0-9_.-]", "");

            if (string.IsNullOrEmpty(dir)) return sanitizedFileName;
            return Path.Combine(dir, sanitizedFileName).Replace('\\', '/');
        }

        public static void SaveAssetBytes(string targetFilePath, byte[] bytes)
        {
            if (string.IsNullOrEmpty(targetFilePath) || bytes == null) return;
            string sanitizedPath = SanitizeAssetPath(targetFilePath);
            string directory = Path.GetDirectoryName(sanitizedPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllBytes(sanitizedPath, bytes);
        }

        public static string TranslateCssProperty(string propertyName, string propertyValue)
        {
            if (string.IsNullOrEmpty(propertyName)) return null;

            string key = propertyName.Trim().ToLowerInvariant();
            if (key == "text-wrap" || key == "-unity-text-wrap")
            {
                string val = (propertyValue ?? "").Trim().ToLowerInvariant();
                if (val == "nowrap" || val == "none")
                {
                    return "white-space: nowrap;";
                }
                return "white-space: normal;";
            }

            if (key == "white-space")
            {
                string val = (propertyValue ?? "").Trim().ToLowerInvariant();
                if (val == "nowrap" || val == "none")
                {
                    return "white-space: nowrap;";
                }
                return "white-space: normal;";
            }

            return $"{propertyName}: {propertyValue};";
        }

        public static string TranslateUnityTextAlign(string horizontalAlign, string verticalAlign)
        {
            string vStr = "middle";
            if (!string.IsNullOrEmpty(verticalAlign))
            {
                switch (verticalAlign.ToUpperInvariant())
                {
                    case "TOP": vStr = "upper"; break;
                    case "CENTER": case "MIDDLE": vStr = "middle"; break;
                    case "BOTTOM": vStr = "lower"; break;
                }
            }

            string hStr = "left";
            if (!string.IsNullOrEmpty(horizontalAlign))
            {
                switch (horizontalAlign.ToUpperInvariant())
                {
                    case "LEFT": hStr = "left"; break;
                    case "CENTER": hStr = "center"; break;
                    case "RIGHT": hStr = "right"; break;
                    case "JUSTIFY": hStr = "left"; break;
                }
            }

            return $"{vStr}-{hStr}";
        }

        public static string ResolveFontDefinitionUrl(TextNode textNode)
        {
            if (textNode == null || string.IsNullOrEmpty(textNode.fontFamily)) return null;

            string assetPath = null;
#if UNITY_EDITOR
            var resolution = Figma2Unity.Editor.Fonts.FontResolver.ResolveFontForTextNode(textNode);
            if (resolution != null && resolution.Success && !resolution.UsedFallback && !string.IsNullOrEmpty(resolution.AssetPath))
            {
                // Prefer the raw .ttf/.otf font asset path for UI Toolkit -unity-font-definition
                assetPath = resolution.AssetPath;
            }
#endif

            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogWarning($"[Figma2Unity FontPipeline] Font definition URL omitted for missing font '{textNode.fontFamily}' on text node '{textNode.name}'. UI Toolkit will fallback to system default font.");
                return null;
            }

            assetPath = assetPath.Replace('\\', '/');
            string fontUrl = assetPath.StartsWith("project://", StringComparison.OrdinalIgnoreCase)
                ? assetPath
                : $"project://database/{assetPath}";

            Debug.Log($"[Figma2Unity FontPipeline] Resolved font definition URL: '{fontUrl}' for font '{textNode.fontFamily}'");
            return fontUrl;
        }

        public static string SanitizeTokenVarName(string tokenName)
        {
            if (string.IsNullOrEmpty(tokenName)) return "default";
            string sanitized = Regex.Replace(tokenName.ToLowerInvariant(), @"[^a-z0-9_-]", "-");
            sanitized = Regex.Replace(sanitized, @"^-+", "");
            sanitized = Regex.Replace(sanitized, @"-+$", "");
            return sanitized;
        }

        public static int ConvertColorChannel(float value)
        {
            return (int)Math.Round(Math.Max(0f, Math.Min(1f, value)) * 255f);
        }
    }

    public static class UnifiedLayoutResolver
    {
        public class LayoutResolutionResult
        {
            public bool InAutoLayoutFlow;
            public bool IsAbsolute;
            public List<string> Rules = new List<string>();
        }

        public static LayoutResolutionResult ResolveLayoutConstraints(IRNode node, bool isRoot, string parentLayoutMode)
        {
            var result = new LayoutResolutionResult();
            if (node == null) return result;

            bool isExplicitAbsolute = string.Equals(node.layoutPositioning, "ABSOLUTE", StringComparison.OrdinalIgnoreCase);
            bool parentHasAutoLayout = !string.IsNullOrEmpty(parentLayoutMode) && !string.Equals(parentLayoutMode, "NONE", StringComparison.OrdinalIgnoreCase);

            // PHASE 1: Strict Decoupling of Positioning
            result.InAutoLayoutFlow = parentHasAutoLayout && !isExplicitAbsolute;
            result.IsAbsolute = isRoot || isExplicitAbsolute || !parentHasAutoLayout;

            if (isRoot)
            {
                result.Rules.Add("position: absolute;");
                result.Rules.Add("left: 0;");
                result.Rules.Add("top: 0;");
                result.Rules.Add("right: 0;");
                result.Rules.Add("bottom: 0;");
                result.Rules.Add("flex-shrink: 0;");
                result.Rules.Add("overflow: hidden;");
            }
            else if (result.InAutoLayoutFlow)
            {
                // Auto Layout Flex Child: position relative to flex container.
                // STRICT RULE: STRIP all left, top, right, bottom absolute coordinates!
                result.Rules.Add("position: relative;");
            }
            else
            {
                // Canvas Absolute Element: position absolute with left/top coordinates
                result.Rules.Add("position: absolute;");
                if (node.bounds != null)
                {
                    result.Rules.Add(string.Format(CultureInfo.InvariantCulture, "left: {0}px;", node.bounds.x));
                    result.Rules.Add(string.Format(CultureInfo.InvariantCulture, "top: {0}px;", node.bounds.y));
                }
            }

            // PHASE 2: Universal Sizing Matrix
            if (!isRoot && node.bounds != null)
            {
                bool fixWidth = true;
                bool fixHeight = true;
                bool autoWidth = false;
                bool autoHeight = false;

                // 1. Self AutoLayout container sizing modes (FIXED, FILL, HUG/AUTO)
                if (node.autoLayout != null && !string.IsNullOrEmpty(node.autoLayout.layoutMode) && !string.Equals(node.autoLayout.layoutMode, "NONE", StringComparison.OrdinalIgnoreCase))
                {
                    string mode = node.autoLayout.layoutMode.ToUpperInvariant();
                    string primarySizing = (node.autoLayout.primaryAxisSizingMode ?? "FIXED").ToUpperInvariant();
                    string counterSizing = (node.autoLayout.counterAxisSizingMode ?? "FIXED").ToUpperInvariant();

                    if (mode == "VERTICAL")
                    {
                        // Primary Axis = Height, Counter Axis = Width
                        if (primarySizing == "HUG" || primarySizing == "AUTO") { fixHeight = false; autoHeight = true; }
                        else if (primarySizing == "FILL") { fixHeight = false; result.Rules.Add("flex-grow: 1;"); }

                        if (counterSizing == "HUG" || counterSizing == "AUTO") { fixWidth = false; autoWidth = true; }
                        else if (counterSizing == "FILL") { fixWidth = false; result.Rules.Add("align-self: stretch;"); }
                    }
                    else if (mode == "HORIZONTAL")
                    {
                        // Primary Axis = Width, Counter Axis = Height
                        if (primarySizing == "HUG" || primarySizing == "AUTO") { fixWidth = false; autoWidth = true; }
                        else if (primarySizing == "FILL") { fixWidth = false; result.Rules.Add("flex-grow: 1;"); }

                        if (counterSizing == "HUG" || counterSizing == "AUTO") { fixHeight = false; autoHeight = true; }
                        else if (counterSizing == "FILL") { fixHeight = false; result.Rules.Add("align-self: stretch;"); }
                    }
                }

                // 2. Child alignment & grow inside AutoLayout parent
                if (result.InAutoLayoutFlow)
                {
                    string childLayoutAlign = (node.layoutAlign ?? "INHERIT").ToUpperInvariant();
                    if (childLayoutAlign == "STRETCH")
                    {
                        result.Rules.Add("align-self: stretch;");
                        if (parentLayoutMode == "VERTICAL") fixWidth = false;
                        else if (parentLayoutMode == "HORIZONTAL") fixHeight = false;
                    }

                    if (node.layoutGrow > 0)
                    {
                        result.Rules.Add("flex-grow: 1;");
                        if (parentLayoutMode == "VERTICAL") fixHeight = false;
                        else if (parentLayoutMode == "HORIZONTAL") fixWidth = false;
                    }
                }

                // Output sizing rules
                if (fixWidth && node.bounds.width >= 0)
                {
                    result.Rules.Add(string.Format(CultureInfo.InvariantCulture, "width: {0}px;", node.bounds.width));
                }
                else if (autoWidth)
                {
                    result.Rules.Add("width: auto;");
                }

                if (fixHeight && node.bounds.height >= 0)
                {
                    result.Rules.Add(string.Format(CultureInfo.InvariantCulture, "height: {0}px;", node.bounds.height));
                }
                else if (autoHeight)
                {
                    result.Rules.Add("height: auto;");
                }
            }

            // PHASE 3: Flex-Direction, Gap & Padding for Container Nodes
            if (node.autoLayout != null && !string.IsNullOrEmpty(node.autoLayout.layoutMode) && !string.Equals(node.autoLayout.layoutMode, "NONE", StringComparison.OrdinalIgnoreCase))
            {
                result.Rules.Add("display: flex;");

                if (string.Equals(node.autoLayout.layoutMode, "HORIZONTAL", StringComparison.OrdinalIgnoreCase))
                {
                    result.Rules.Add("flex-direction: row;");
                }
                else if (string.Equals(node.autoLayout.layoutMode, "VERTICAL", StringComparison.OrdinalIgnoreCase))
                {
                    result.Rules.Add("flex-direction: column;");
                }

                if (node.autoLayout.gap > 0)
                {
                    result.Rules.Add(string.Format(CultureInfo.InvariantCulture, "gap: {0}px;", node.autoLayout.gap));
                }

                if (node.autoLayout.padding != null)
                {
                    var pad = node.autoLayout.padding;
                    if (pad.top > 0) result.Rules.Add(string.Format(CultureInfo.InvariantCulture, "padding-top: {0}px;", pad.top));
                    if (pad.right > 0) result.Rules.Add(string.Format(CultureInfo.InvariantCulture, "padding-right: {0}px;", pad.right));
                    if (pad.bottom > 0) result.Rules.Add(string.Format(CultureInfo.InvariantCulture, "padding-bottom: {0}px;", pad.bottom));
                    if (pad.left > 0) result.Rules.Add(string.Format(CultureInfo.InvariantCulture, "padding-left: {0}px;", pad.left));
                }

                if (!string.IsNullOrEmpty(node.autoLayout.primaryAxisAlign))
                {
                    switch (node.autoLayout.primaryAxisAlign.ToUpperInvariant())
                    {
                        case "MIN": result.Rules.Add("justify-content: flex-start;"); break;
                        case "CENTER": result.Rules.Add("justify-content: center;"); break;
                        case "MAX": result.Rules.Add("justify-content: flex-end;"); break;
                        case "SPACE_BETWEEN": result.Rules.Add("justify-content: space-between;"); break;
                    }
                }

                if (!string.IsNullOrEmpty(node.autoLayout.counterAxisAlign))
                {
                    switch (node.autoLayout.counterAxisAlign.ToUpperInvariant())
                    {
                        case "MIN": result.Rules.Add("align-items: flex-start;"); break;
                        case "CENTER": result.Rules.Add("align-items: center;"); break;
                        case "MAX": result.Rules.Add("align-items: flex-end;"); break;
                        case "STRETCH": case "BASELINE": result.Rules.Add("align-items: stretch;"); break;
                    }
                }
            }

            return result;
        }
    }
}
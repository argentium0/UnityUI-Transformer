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
                    SyncPackageImporter.ResolveTextNodeFonts(document, fontsFolder);

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
            var sb = new StringBuilder();
            sb.AppendLine("/* Automatically generated by Figma2Unity Importer */");
            sb.AppendLine();

            // 1. Output :root custom properties block for design tokens
            if (document?.tokens != null)
            {
                bool hasTokens = false;
                var rootTokensSb = new StringBuilder();
                rootTokensSb.AppendLine(":root {");

                if (document.tokens.colors != null && document.tokens.colors.Count > 0)
                {
                    foreach (var cToken in document.tokens.colors)
                    {
                        if (cToken.value != null)
                        {
                            hasTokens = true;
                            string varName = SanitizeTokenVarName(cToken.name ?? cToken.id);
                            int r = ConvertColorChannel(cToken.value.r);
                            int g = ConvertColorChannel(cToken.value.g);
                            int b = ConvertColorChannel(cToken.value.b);
                            rootTokensSb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    --color-{0}: rgba({1}, {2}, {3}, {4:F2});", varName, r, g, b, cToken.value.a));
                        }
                    }
                }

                if (document.tokens.spacing != null && document.tokens.spacing.Count > 0)
                {
                    foreach (var sToken in document.tokens.spacing)
                    {
                        hasTokens = true;
                        string varName = SanitizeTokenVarName(sToken.name ?? sToken.id);
                        rootTokensSb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    --spacing-{0}: {1}px;", varName, sToken.value));
                    }
                }

                if (document.tokens.typography != null && document.tokens.typography.Count > 0)
                {
                    foreach (var tToken in document.tokens.typography)
                    {
                        if (tToken.fontSize > 0)
                        {
                            hasTokens = true;
                            string varName = SanitizeTokenVarName(tToken.name ?? tToken.id);
                            rootTokensSb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    --font-size-{0}: {1}px;", varName, tToken.fontSize));
                        }
                    }
                }

                rootTokensSb.AppendLine("}");

                if (hasTokens)
                {
                    sb.Append(rootTokensSb.ToString());
                    sb.AppendLine();
                }
            }

            var generatedClasses = new HashSet<string>();

            if (document?.rootNodes != null)
            {
                foreach (var rootNode in document.rootNodes)
                {
                    // Pass true for isRoot on top-level frames, "NONE" for parentLayoutMode
                    GenerateNodeStylesRecursive(rootNode, sb, generatedClasses, document, true, "NONE", packageName);
                }
            }

            return sb.ToString();
        }

        private static void GenerateNodeStylesRecursive(
            IRNode node,
            StringBuilder sb,
            HashSet<string> generatedClasses,
            IRDocument document,
            bool isRoot,
            string parentLayoutMode,
            string packageName)
        {
            if (node == null) return;

            string className = USSStyleGenerator.SanitizeClassName(node.name, node.id);
            if (!generatedClasses.Contains(className))
            {
                generatedClasses.Add(className);
                sb.AppendLine($".{className} {{");

                // 1. POSITIONING & BOUNDS
                bool isAbsolute = (node.layoutPositioning == "ABSOLUTE");
                bool parentLacksAutoLayout = (parentLayoutMode == "NONE");

                // Prioritize standard Flexbox flow over raw absolute coordinates
                bool inAutoLayoutFlow = !parentLacksAutoLayout && !isAbsolute;

                if (isRoot)
                {
                    sb.AppendLine("    position: absolute;");
                    sb.AppendLine("    left: 0;");
                    sb.AppendLine("    top: 0;");
                    sb.AppendLine("    right: 0;");
                    sb.AppendLine("    bottom: 0;");
                    sb.AppendLine("    flex-shrink: 0;");
                    sb.AppendLine("    overflow: hidden;");
                }
                else if (inAutoLayoutFlow)
                {
                    // Child inside an Auto-Layout parent participating in flex flow
                    sb.AppendLine("    position: relative;");
                    if (node.autoLayout != null)
                    {
                        if (node.autoLayout.layoutGrow > 0)
                        {
                            sb.AppendLine("    flex-grow: 1;");
                        }
                        if (node.autoLayout.layoutAlign == "STRETCH")
                        {
                            sb.AppendLine("    align-self: stretch;");
                        }
                    }
                }
                else
                {
                    // Non-auto-layout child OR explicitly absolute-positioned child OR Group inside free-flow
                    sb.AppendLine("    position: absolute;");
                    if (node.bounds != null)
                    {
                        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    left: {0}px;", node.bounds.x));
                        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    top: {0}px;", node.bounds.y));
                    }
                }

                if (!isRoot && node.bounds != null)
                {
                    bool fixWidth = true;
                    bool fixHeight = true;

                    // P1 Fix 5: Read layoutAlign from the node directly, not from node.autoLayout
                    if (inAutoLayoutFlow)
                    {
                        string childLayoutAlign = node.layoutAlign ?? "INHERIT";
                        
                        if (parentLayoutMode == "VERTICAL")
                        {
                            // In vertical container, width is cross-axis
                            if (childLayoutAlign == "STRETCH") fixWidth = false;
                            // If child has layoutGrow > 0, it grows in height (primary axis)
                            if (node.autoLayout != null && node.autoLayout.layoutGrow > 0) fixHeight = false;
                        }
                        else if (parentLayoutMode == "HORIZONTAL")
                        {
                            // In horizontal container, height is cross-axis
                            if (childLayoutAlign == "STRETCH") fixHeight = false;
                            // If child has layoutGrow > 0, it grows in width (primary axis)
                            if (node.autoLayout != null && node.autoLayout.layoutGrow > 0) fixWidth = false;
                        }
                    }

                    if (fixWidth && node.bounds.width >= 0)
                    {
                        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    width: {0}px;", node.bounds.width));
                    }
                    if (fixHeight && node.bounds.height >= 0)
                    {
                        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    height: {0}px;", node.bounds.height));
                    }
                }

                // 1.5 AUTO-LAYOUT FLEXBOX PROPERTIES FOR CONTAINERS
                bool isAutoLayoutContainer = node.autoLayout != null && !string.IsNullOrEmpty(node.autoLayout.layoutMode) && node.autoLayout.layoutMode != "NONE";
                if (isAutoLayoutContainer)
                {
                    sb.AppendLine("    display: flex;");

                    if (node.autoLayout.layoutMode.Equals("HORIZONTAL", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.AppendLine("    flex-direction: row;");
                    }
                    else if (node.autoLayout.layoutMode.Equals("VERTICAL", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.AppendLine("    flex-direction: column;");
                    }

                    if (node.autoLayout.gap > 0)
                    {
                        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    gap: {0}px;", node.autoLayout.gap));
                    }

                    if (node.autoLayout.padding != null)
                    {
                        var pad = node.autoLayout.padding;
                        if (pad.top > 0) sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    padding-top: {0}px;", pad.top));
                        if (pad.right > 0) sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    padding-right: {0}px;", pad.right));
                        if (pad.bottom > 0) sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    padding-bottom: {0}px;", pad.bottom));
                        if (pad.left > 0) sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    padding-left: {0}px;", pad.left));
                    }

                    if (!string.IsNullOrEmpty(node.autoLayout.primaryAxisAlign))
                    {
                        switch (node.autoLayout.primaryAxisAlign.ToUpperInvariant())
                        {
                            case "MIN":
                                sb.AppendLine("    justify-content: flex-start;");
                                break;
                            case "CENTER":
                                sb.AppendLine("    justify-content: center;");
                                break;
                            case "MAX":
                                sb.AppendLine("    justify-content: flex-end;");
                                break;
                            case "SPACE_BETWEEN":
                                sb.AppendLine("    justify-content: space-between;");
                                break;
                        }
                    }

                    if (!string.IsNullOrEmpty(node.autoLayout.counterAxisAlign))
                    {
                        switch (node.autoLayout.counterAxisAlign.ToUpperInvariant())
                        {
                            case "MIN":
                                sb.AppendLine("    align-items: flex-start;");
                                break;
                            case "CENTER":
                                sb.AppendLine("    align-items: center;");
                                break;
                            case "MAX":
                                sb.AppendLine("    align-items: flex-end;");
                                break;
                            case "STRETCH":
                            case "BASELINE":
                                sb.AppendLine("    align-items: stretch;");
                                break;
                        }
                    }
                }

                // 2. IMAGE & VECTOR ASSET LINKING
                string pkg = string.IsNullOrEmpty(packageName) ? "SyncPackage" : packageName;

                // For rasterized nodes that map to an image fill (like 'Rectangle 1')
                bool hasImageFill = node.fills != null && node.fills.Exists(f => f.type == "IMAGE");
                string imageRef = (node as ImageNode)?.imageAssetRef;
                
                // P0 Fix 3: FrameNodes with IMAGE fills also carry imageAssetRef
                if (string.IsNullOrEmpty(imageRef) && node is FrameNode frameNodeImg)
                {
                    imageRef = frameNodeImg.imageAssetRef;
                }
                
                if (string.IsNullOrEmpty(imageRef) && hasImageFill)
                {
                    string sanitizedId = node.id.Replace(":", "_").Replace("/", "_");
                    imageRef = $"images/{sanitizedId}_1x.png";
                }

                if (!string.IsNullOrEmpty(imageRef))
                {
                    string cleanAssetRef = SanitizeAssetPath(imageRef);
                    string relAssetPath = $"Assets/Figma2UnityImports/{pkg}/{cleanAssetRef}".Replace('\\', '/');
                    string targetUrl = $"project://database/{relAssetPath}";
                    
                    sb.AppendLine($"    background-image: url('{targetUrl}');");
                    sb.AppendLine("    -unity-background-scale-mode: stretch-to-fill;");
                }
                else if (node is VectorNode vecNode && !string.IsNullOrEmpty(vecNode.svgAssetRef))
                {
                    string cleanAssetRef = SanitizeAssetPath(vecNode.svgAssetRef);
                    string relAssetPath = $"Assets/Figma2UnityImports/{pkg}/{cleanAssetRef}".Replace('\\', '/');
                    string targetUrl = $"project://database/{relAssetPath}";
                    
                    sb.AppendLine($"    background-image: url('{targetUrl}');");
                    sb.AppendLine("    -unity-background-scale-mode: scale-to-fit;");
                }

                // 2.5 FILLS & TRANSPARENT BACKGROUNDS
                bool hasBgFill = false;
                if (node.fills != null && node.fills.Count > 0)
                {
                    foreach (var fill in node.fills)
                    {
                        // Skip if it's an image or gradient, handled elsewhere
                        if (fill.type == "IMAGE" || fill.type == "GRADIENT") continue;

                        ColorValue colorVal = fill.color;
                        string matchedTokenVar = null;

                        // Check explicit fill.tokenId match
                        if (!string.IsNullOrEmpty(fill.tokenId) && document?.tokens?.colors != null)
                        {
                            var token = document.tokens.colors.Find(t => t.id == fill.tokenId);
                            if (token != null)
                            {
                                colorVal = token.value;
                                matchedTokenVar = $"var(--color-{SanitizeTokenVarName(token.name ?? token.id)})";
                            }
                        }

                        // Fallback matching color by value
                        if (matchedTokenVar == null && colorVal != null && document?.tokens?.colors != null)
                        {
                            var token = document.tokens.colors.Find(t => t.value != null &&
                                Math.Abs(t.value.r - colorVal.r) < 0.01f &&
                                Math.Abs(t.value.g - colorVal.g) < 0.01f &&
                                Math.Abs(t.value.b - colorVal.b) < 0.01f &&
                                Math.Abs(t.value.a - colorVal.a) < 0.01f);
                            if (token != null)
                            {
                                matchedTokenVar = $"var(--color-{SanitizeTokenVarName(token.name ?? token.id)})";
                            }
                        }

                        if (matchedTokenVar != null || colorVal != null)
                        {
                            float alpha = fill.opacity.HasValue ? fill.opacity.Value : (colorVal?.a ?? 1f);
                            string colorValueStr;

                            if (matchedTokenVar != null)
                            {
                                colorValueStr = matchedTokenVar;
                            }
                            else
                            {
                                int r = ConvertColorChannel(colorVal.r);
                                int g = ConvertColorChannel(colorVal.g);
                                int b = ConvertColorChannel(colorVal.b);
                                colorValueStr = string.Format(CultureInfo.InvariantCulture, "rgba({0}, {1}, {2}, {3:F2})", r, g, b, alpha);
                            }

                            if (node is TextNode)
                            {
                                sb.AppendLine($"    color: {colorValueStr};");
                            }
                            else
                            {
                                sb.AppendLine($"    background-color: {colorValueStr};");
                                hasBgFill = true;
                            }
                            break; // Stop after first solid color
                        }
                    }
                }

                // FORCE TEXT TRANSPARENCY & UNFILLED TRANSPARENCY
                if (!hasBgFill)
                {
                    // Avoid appending transparent background if a solid color was already applied
                    sb.AppendLine("    background-color: transparent;");
                }

                // 3. FORCE BORDER RADIUS FOR ELLIPSE & OTHER SHAPES
                if (node is EllipseNode)
                {
                    sb.AppendLine("    border-radius: 50%;");
                }
                else if (node.cornerRadius != null)
                {
                    var r = node.cornerRadius;
                    if (r.topLeft > 0 || r.topRight > 0 || r.bottomRight > 0 || r.bottomLeft > 0)
                    {
                        if (r.topLeft == r.topRight && r.topRight == r.bottomRight && r.bottomRight == r.bottomLeft)
                        {
                            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    border-radius: {0}px;", r.topLeft));
                        }
                        else
                        {
                            if (r.topLeft > 0) sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    border-top-left-radius: {0}px;", r.topLeft));
                            if (r.topRight > 0) sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    border-top-right-radius: {0}px;", r.topRight));
                            if (r.bottomRight > 0) sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    border-bottom-right-radius: {0}px;", r.bottomRight));
                            if (r.bottomLeft > 0) sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    border-bottom-left-radius: {0}px;", r.bottomLeft));
                        }
                    }
                }

                // 4. STROKES (BORDER)
                if (node.strokes != null && node.strokes.Count > 0)
                {
                    var stroke = node.strokes[0];
                    if (stroke.weight > 0)
                    {
                        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    border-width: {0}px;", stroke.weight));
                        ColorValue strokeColor = stroke.color;
                        string matchedStrokeTokenVar = null;

                        if (!string.IsNullOrEmpty(stroke.tokenId) && document?.tokens?.colors != null)
                        {
                            var token = document.tokens.colors.Find(t => t.id == stroke.tokenId);
                            if (token != null)
                            {
                                strokeColor = token.value;
                                matchedStrokeTokenVar = $"var(--color-{SanitizeTokenVarName(token.name ?? token.id)})";
                            }
                        }

                        if (matchedStrokeTokenVar != null)
                        {
                            sb.AppendLine($"    border-color: {matchedStrokeTokenVar};");
                        }
                        else if (strokeColor != null)
                        {
                            int r = ConvertColorChannel(strokeColor.r);
                            int g = ConvertColorChannel(strokeColor.g);
                            int b = ConvertColorChannel(strokeColor.b);
                            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    border-color: rgba({0}, {1}, {2}, {3:F2});", r, g, b, strokeColor.a));
                        }
                    }
                }

                // 5. OPACITY
                if (node.opacity < 1.0f)
                {
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    opacity: {0:F2};", node.opacity));
                }

                // 6. TYPOGRAPHY & TEXT WRAPPING FOR TEXT NODES
                if (node is TextNode textNode)
                {
                    sb.AppendLine("    white-space: normal;");

                    float? fontSize = textNode.fontSize;
                    string matchedFontSizeVar = null;

                    if (!string.IsNullOrEmpty(textNode.typographyTokenId) && document?.tokens?.typography != null)
                    {
                        var topoToken = document.tokens.typography.Find(t => t.id == textNode.typographyTokenId);
                        if (topoToken != null && topoToken.fontSize > 0)
                        {
                            fontSize = topoToken.fontSize;
                            matchedFontSizeVar = $"var(--font-size-{SanitizeTokenVarName(topoToken.name ?? topoToken.id)})";
                        }
                    }

                    if (matchedFontSizeVar != null)
                    {
                        sb.AppendLine($"    font-size: {matchedFontSizeVar};");
                    }
                    else if (fontSize.HasValue && fontSize.Value > 0)
                    {
                        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    font-size: {0}px;", fontSize.Value));
                    }

                    if (!string.IsNullOrEmpty(textNode.fontFamily))
                    {
                        string fontUrl = ResolveFontDefinitionUrl(textNode);
                        if (!string.IsNullOrEmpty(fontUrl))
                        {
                            sb.AppendLine($"    -unity-font-definition: url('{fontUrl}');");
                        }
                    }

                    if (textNode.fontWeight.HasValue && textNode.fontWeight.Value >= 600)
                    {
                        sb.AppendLine("    -unity-font-style: bold;");
                    }

                    string unityTextAlign = TranslateUnityTextAlign(textNode.textAlign, textNode.textAlignVertical);
                    sb.AppendLine($"    -unity-text-align: {unityTextAlign};");

                    if (string.Equals(textNode.textDecoration, "UNDERLINE", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.AppendLine("    border-bottom-width: 1px;");
                        sb.AppendLine("    border-bottom-color: initial;");
                    }
                }

                sb.AppendLine("}");
                sb.AppendLine();
            }

            // Recurse children passing current layout mode
            string currentLayoutMode = (node.autoLayout != null && !string.IsNullOrEmpty(node.autoLayout.layoutMode)) ? node.autoLayout.layoutMode : "NONE";

            if (node is FrameNode frameNode && frameNode.children != null)
            {
                foreach (var child in frameNode.children)
                {
                    GenerateNodeStylesRecursive(child, sb, generatedClasses, document, false, currentLayoutMode, packageName);
                }
            }
            else if (node is GroupNode groupNode && groupNode.children != null)
            {
                foreach (var child in groupNode.children)
                {
                    GenerateNodeStylesRecursive(child, sb, generatedClasses, document, false, currentLayoutMode, packageName);
                }
            }
            else if (node is ComponentInstanceNode compNode && compNode.children != null)
            {
                foreach (var child in compNode.children)
                {
                    GenerateNodeStylesRecursive(child, sb, generatedClasses, document, false, currentLayoutMode, packageName);
                }
            }
            else if (node is UnsupportedNode unsupNode && unsupNode.children != null)
            {
                foreach (var child in unsupNode.children)
                {
                    GenerateNodeStylesRecursive(child, sb, generatedClasses, document, false, currentLayoutMode, packageName);
                }
            }
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
                // Ensure assetPath is a valid standard font asset and not a TMP_FontAsset (.asset) or LiberationSans SDF
                if (!resolution.AssetPath.EndsWith("SDF.asset", StringComparison.OrdinalIgnoreCase) &&
                    !resolution.AssetPath.Contains("LiberationSans"))
                {
                    assetPath = resolution.AssetPath;
                }
            }
#endif

            if (string.IsNullOrEmpty(assetPath))
            {
                // Omit font definition when custom font is missing so UI Toolkit falls back to system default font cleanly
                return null;
            }

            assetPath = assetPath.Replace('\\', '/');
            return assetPath.StartsWith("project://", StringComparison.OrdinalIgnoreCase)
                ? assetPath
                : $"project://database/{assetPath}";
        }

        public static string SanitizeTokenVarName(string tokenName)
        {
            if (string.IsNullOrEmpty(tokenName)) return "default";
            string sanitized = Regex.Replace(tokenName.ToLowerInvariant(), @"[^a-z0-9_-]", "-");
            sanitized = Regex.Replace(sanitized, @"^-+", "");
            sanitized = Regex.Replace(sanitized, @"-+$", "");
            return sanitized;
        }

        private static int ConvertColorChannel(float value)
        {
            return (int)Math.Round(Math.Max(0f, Math.Min(1f, value)) * 255f);
        }
    }
}
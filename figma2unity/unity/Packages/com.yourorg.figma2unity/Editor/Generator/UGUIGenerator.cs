using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Figma2Unity.Editor.Schema;
using Figma2Unity.Editor.Reporting;
using Figma2Unity.Editor.Fonts;
using Figma2Unity.Tokens;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Figma2Unity.Editor.Generator
{
    public enum UIExporterTarget
    {
        UIToolkit_UXML,
        uGUI_Prefab
    }

    public static class UGUIGenerator
    {
        public class UGUIGenerationResult
        {
            public bool Success;
            public List<string> PrefabPaths = new List<string>();
        }

        public static UGUIGenerationResult Generate(IRDocument document, string destinationFolder, string packageName)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (string.IsNullOrEmpty(destinationFolder))
            {
                destinationFolder = Path.Combine("Assets", "Figma2Unity", "Generated", packageName ?? "SyncPackage");
            }

            string prefabsFolder = Path.Combine(destinationFolder, "Prefabs");
            if (!Directory.Exists(prefabsFolder))
            {
                Directory.CreateDirectory(prefabsFolder);
            }

            var result = new UGUIGenerationResult();

            if (document.rootNodes == null) return result;

            for (int i = 0; i < document.rootNodes.Count; i++)
            {
                var rootNode = document.rootNodes[i];
                string screenName = USSStyleGenerator.SanitizeClassName(rootNode.name ?? $"Screen_{i}", rootNode.id);
                string prefabFileName = $"{screenName}.prefab";
                string prefabFullPath = Path.Combine(prefabsFolder, prefabFileName);

                GameObject rootGo = BuildRootCanvasGameObject(rootNode, document, packageName);

#if UNITY_EDITOR
                string unityAssetPath = prefabFullPath.Replace('\\', '/');
                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(rootGo, unityAssetPath);
                if (savedPrefab != null)
                {
                    result.PrefabPaths.Add(unityAssetPath);
                }
                UnityEngine.Object.DestroyImmediate(rootGo);
#endif
            }

            result.Success = true;

#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif

            return result;
        }

        private static GameObject BuildRootCanvasGameObject(IRNode rootNode, IRDocument document, string packageName)
        {
            float width = rootNode.bounds?.width ?? 1920f;
            float height = rootNode.bounds?.height ?? 1080f;

            string rootName = rootNode.name ?? "CanvasScreen";
            GameObject canvasGo = new GameObject(rootName);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(width, height);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;
            canvasRect.pivot = new Vector2(0.5f, 0.5f);

            // Log root frame
            FigmaImportLogger.LogNodeProcessed(rootNode.type);

            // Build hierarchy children inside Canvas
            if (rootNode is FrameNode frameNode && frameNode.children != null)
            {
                foreach (var child in frameNode.children)
                {
                    BuildGameObjectRecursive(child, canvasGo.transform, document, width, height);
                }
            }
            else if (rootNode is GroupNode groupNode && groupNode.children != null)
            {
                foreach (var child in groupNode.children)
                {
                    BuildGameObjectRecursive(child, canvasGo.transform, document, width, height);
                }
            }

            return canvasGo;
        }

        private static GameObject BuildGameObjectRecursive(IRNode node, Transform parentTransform, IRDocument document, float parentWidth, float parentHeight)
        {
            if (node == null || !node.visible) return null;

            FigmaImportLogger.LogNodeProcessed(node.type);

            string objectName = node.name ?? "Node";
            GameObject go = new GameObject(objectName);
            go.transform.SetParent(parentTransform, false);

            RectTransform rect = go.AddComponent<RectTransform>();

            float nodeW = node.bounds?.width ?? 100f;
            float nodeH = node.bounds?.height ?? 100f;
            float nodeX = node.bounds?.x ?? 0f;
            float nodeY = node.bounds?.y ?? 0f;

            // 1. RECTTRANSFORM MATH (Constraints & Positioning)
            ApplyRectTransformMath(rect, node, nodeX, nodeY, nodeW, nodeH, parentWidth, parentHeight);

            // 2. AUTO-LAYOUT LAYOUTGROUP
            if (node.autoLayout != null && !string.IsNullOrEmpty(node.autoLayout.layoutMode) && node.autoLayout.layoutMode != "NONE")
            {
                ApplyAutoLayoutGroup(go, node.autoLayout);
            }

            // 3. COMPONENT & TOKEN MAPPING
            if (node is TextNode textNode)
            {
                ApplyTextComponent(go, textNode, document);
            }
            else if (node is ImageNode imageNode)
            {
                FigmaImportLogger.LogRasterizedNode(node.id, node.name, node.type, "Image fill rasterization");
                ApplyImageComponent(go, node, document, true);
            }
            else if (node is VectorNode vectorNode)
            {
                FigmaImportLogger.LogRasterizedNode(node.id, node.name, node.type, "Vector shape rasterization");
                ApplyImageComponent(go, node, document, true);
            }
            else if (node is UnsupportedNode unsupportedNode)
            {
                FigmaImportLogger.LogRasterizedNode(node.id, node.name, node.type, "Unsupported node type fallback to uGUI Image");
                ApplyImageComponent(go, node, document, true);
            }
            else
            {
                ApplyImageComponent(go, node, document, false);
            }

            // Recurse children
            if (node is FrameNode frame && frame.children != null)
            {
                foreach (var child in frame.children)
                {
                    BuildGameObjectRecursive(child, go.transform, document, nodeW, nodeH);
                }
            }
            else if (node is GroupNode group && group.children != null)
            {
                foreach (var child in group.children)
                {
                    BuildGameObjectRecursive(child, go.transform, document, nodeW, nodeH);
                }
            }
            else if (node is ComponentInstanceNode comp && comp.children != null)
            {
                foreach (var child in comp.children)
                {
                    BuildGameObjectRecursive(child, go.transform, document, nodeW, nodeH);
                }
            }

            return go;
        }

        private static void ApplyRectTransformMath(RectTransform rect, IRNode node, float x, float y, float w, float h, float pw, float ph)
        {
            rect.pivot = new Vector2(0f, 1f); // Top-Left pivot

            string hConstraint = node.constraints?.horizontal ?? "MIN";
            string vConstraint = node.constraints?.vertical ?? "MIN";

            if (hConstraint == "STRETCH" && vConstraint == "STRETCH" && pw > 0 && ph > 0)
            {
                rect.anchorMin = new Vector2(x / pw, 1f - (y + h) / ph);
                rect.anchorMax = new Vector2((x + w) / pw, 1f - y / ph);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                // Default Top-Left relative positioning
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(x, -y);
                rect.sizeDelta = new Vector2(w, h);
            }
        }

        private static void ApplyAutoLayoutGroup(GameObject go, AutoLayout autoLayout)
        {
            if (autoLayout.layoutMode.Equals("HORIZONTAL", StringComparison.OrdinalIgnoreCase))
            {
                var hlg = go.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = autoLayout.gap;
                if (autoLayout.padding != null)
                {
                    hlg.padding = new RectOffset((int)autoLayout.padding.left, (int)autoLayout.padding.right, (int)autoLayout.padding.top, (int)autoLayout.padding.bottom);
                }
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
            }
            else if (autoLayout.layoutMode.Equals("VERTICAL", StringComparison.OrdinalIgnoreCase))
            {
                var vlg = go.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = autoLayout.gap;
                if (autoLayout.padding != null)
                {
                    vlg.padding = new RectOffset((int)autoLayout.padding.left, (int)autoLayout.padding.right, (int)autoLayout.padding.top, (int)autoLayout.padding.bottom);
                }
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
            }

            var csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = autoLayout.primaryAxisSizingMode == "AUTO" ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = autoLayout.counterAxisSizingMode == "AUTO" ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
        }

        private static void ApplyTextComponent(GameObject go, TextNode textNode, IRDocument document)
        {
            Type tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro") ?? Type.GetType("TMPro.TextMeshProUGUI");
            Component tmpComponent = null;

            if (tmpType != null)
            {
                tmpComponent = go.AddComponent(tmpType);
                var textProp = tmpType.GetProperty("text");
                if (textProp != null)
                {
                    textProp.SetValue(tmpComponent, textNode.characters ?? string.Empty);
                }

                var fontRes = FontResolver.ResolveFontForTextNode(textNode, null);
                if (fontRes.FontAsset != null)
                {
                    var fontProp = tmpType.GetProperty("font");
                    if (fontProp != null)
                    {
                        fontProp.SetValue(tmpComponent, fontRes.FontAsset);
                    }
                }

                float fontSize = textNode.fontSize ?? 18f;
                if (!string.IsNullOrEmpty(textNode.typographyTokenId) && document?.tokens?.typography != null)
                {
                    var topoToken = document.tokens.typography.Find(t => t.id == textNode.typographyTokenId);
                    if (topoToken != null && topoToken.fontSize > 0)
                    {
                        fontSize = topoToken.fontSize;
                    }
                }

                var sizeProp = tmpType.GetProperty("fontSize");
                if (sizeProp != null)
                {
                    sizeProp.SetValue(tmpComponent, fontSize);
                }

                Color textColor = Color.white;
                if (textNode.fills != null && textNode.fills.Count > 0 && textNode.fills[0].color != null)
                {
                    textColor = textNode.fills[0].color.ToUnityColor();
                }

                var colorProp = tmpType.GetProperty("color");
                if (colorProp != null)
                {
                    colorProp.SetValue(tmpComponent, textColor);
                }
            }
            else
            {
                // Fallback to standard UnityEngine.UI.Text if TextMeshPro is not present
                var textComp = go.AddComponent<Text>();
                textComp.text = textNode.characters ?? string.Empty;
                textComp.fontSize = (int)(textNode.fontSize ?? 18f);
                if (textNode.fills != null && textNode.fills.Count > 0 && textNode.fills[0].color != null)
                {
                    textComp.color = textNode.fills[0].color.ToUnityColor();
                }
            }
        }

        private static void ApplyImageComponent(GameObject go, IRNode node, IRDocument document, bool isRasterizedAsset)
        {
            Image img = go.AddComponent<Image>();

            bool hasColor = false;
            if (node.fills != null && node.fills.Count > 0)
            {
                var fill = node.fills[0];
                if (fill.color != null)
                {
                    img.color = fill.color.ToUnityColor();
                    hasColor = true;
                }
            }

            if (!hasColor && !(node is TextNode))
            {
                img.color = Color.clear; // Transparent background for unfilled containers
            }
        }
    }
}

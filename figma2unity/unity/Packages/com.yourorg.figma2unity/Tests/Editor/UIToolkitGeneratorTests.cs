using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Figma2Unity.Editor.Generator;
using Figma2Unity.Editor.Schema;

namespace Figma2Unity.Tests.Editor
{
    public class UIToolkitGeneratorTests
    {
        [Test]
        public void SanitizeClassName_SanitizesSpecialCharactersAndPrependsID()
        {
            string className = USSStyleGenerator.SanitizeClassName("Card Header / Title", "10:2");
            Assert.AreEqual("card-header---title-10_2", className);
        }

        [Test]
        public void USSStyleGenerator_AutoLayoutProperties_GeneratesCorrectFlexboxRules()
        {
            var doc = new IRDocument
            {
                rootNodes = new List<IRNode>
                {
                    new FrameNode
                    {
                        id = "1:1",
                        name = "FlexFrame",
                        type = "FRAME",
                        autoLayout = new AutoLayout
                        {
                            layoutMode = "HORIZONTAL",
                            primaryAxisAlign = "CENTER",
                            counterAxisAlign = "MIN",
                            gap = 16f,
                            padding = new Padding { top = 10, right = 20, bottom = 10, left = 20 },
                            layoutGrow = 1f
                        },
                        fills = new List<Fill>
                        {
                            new Fill { type = "SOLID", color = new ColorValue { r = 0.1f, g = 0.5f, b = 0.9f, a = 1.0f } }
                        },
                        cornerRadius = new CornerRadius { topLeft = 8, topRight = 8, bottomRight = 0, bottomLeft = 0 }
                    }
                }
            };

            string uss = USSStyleGenerator.GenerateUSS(doc, "TestPackage");

            Assert.IsNotNull(uss);
            Assert.Contains("flex-direction: row;", uss);
            Assert.Contains("justify-content: center;", uss);
            Assert.Contains("align-items: flex-start;", uss);
            Assert.Contains("gap: 16px;", uss);
            Assert.Contains("padding-top: 10px;", uss);
            Assert.Contains("padding-right: 20px;", uss);
            Assert.Contains("flex-grow: 1;", uss);
            Assert.Contains("background-color: rgba(26, 128, 230, 1.00);", uss);
            Assert.Contains("border-top-left-radius: 8px;", uss);
        }

        [Test]
        public void UXMLTreeGenerator_IRNodeHierarchy_GeneratesValidUXMLWithElements()
        {
            var frame = new FrameNode
            {
                id = "10:10",
                name = "MainContainer",
                type = "FRAME",
                children = new List<IRNode>
                {
                    new TextNode
                    {
                        id = "10:11",
                        name = "HeaderLabel",
                        type = "TEXT",
                        characters = "Welcome to Figma2Unity"
                    },
                    new ImageNode
                    {
                        id = "10:12",
                        name = "AvatarImage",
                        type = "IMAGE",
                        imageAssetRef = "exports/images/10_12@1x.png"
                    },
                    new UnsupportedNode
                    {
                        id = "10:13",
                        name = "Unsupported3DWidget",
                        type = "UNSUPPORTED",
                        figmaNodeType = "WIDGET"
                    }
                }
            };

            string uxml = UXMLTreeGenerator.GenerateUXML(frame, "Assets/Figma2Unity/Generated/TestPackage/TestPackage.uss");

            Assert.IsNotNull(uxml);
            Assert.IsFalse(uxml.StartsWith("<?xml"), "Generated UXML should omit <?xml ... ?> declaration.");
            Assert.IsFalse(uxml.Contains("utf-16"), "Generated UXML should not contain utf-16 encoding declaration.");
            Assert.Contains("<ui:Style src=\"Assets/Figma2Unity/Generated/TestPackage/TestPackage.uss\" />", uxml);
            Assert.Contains("<ui:VisualElement name=\"MainContainer\"", uxml);
            Assert.Contains("<ui:Label text=\"Welcome to Figma2Unity\" name=\"HeaderLabel\"", uxml);
            Assert.Contains("<ui:Image name=\"AvatarImage\"", uxml);
            Assert.Contains("<ui:Image name=\"Unsupported3DWidget\"", uxml);
        }

        [Test]
        public void UIToolkitGenerator_Generate_WritesFilesToDestinationFolder()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "Figma2UnityGenTest_" + System.Guid.NewGuid().ToString("N"));

            var doc = new IRDocument
            {
                rootNodes = new List<IRNode>
                {
                    new FrameNode
                    {
                        id = "100:1",
                        name = "HomeScreen",
                        type = "FRAME"
                    }
                }
            };

            var result = UIToolkitGenerator.Generate(doc, tempDir, "TestPkg");

            Assert.IsTrue(result.Success);
            Assert.IsTrue(File.Exists(result.USSPath));
            Assert.AreEqual(1, result.UXMLPaths.Count);
            Assert.IsTrue(File.Exists(result.UXMLPaths[0]));

            string uxmlContent = File.ReadAllText(result.UXMLPaths[0]);
            Assert.IsFalse(uxmlContent.StartsWith("<?xml"), "Generated UXML file should not contain xml declaration line.");
            Assert.IsFalse(uxmlContent.Contains("utf-16"), "Generated UXML file should not contain utf-16 encoding declaration.");

            // Cleanup temp directory
            Directory.Delete(tempDir, true);
        }

        [Test]
        public void USSStyleGenerator_VisualNodeMapping_GeneratesCorrectStyles()
        {
            var doc = new IRDocument
            {
                rootNodes = new List<IRNode>
                {
                    new RectangleNode
                    {
                        id = "2:1",
                        name = "GreenRect",
                        type = "RECTANGLE",
                        bounds = new Bounds { x = 10f, y = 20f, width = 120f, height = 80f },
                        fills = new List<Fill>
                        {
                            new Fill { type = "SOLID", color = new ColorValue { r = 0f, g = 1f, b = 0f, a = 1f } }
                        },
                        cornerRadius = new CornerRadius { topLeft = 4, topRight = 4, bottomRight = 4, bottomLeft = 4 }
                    },
                    new EllipseNode
                    {
                        id = "2:2",
                        name = "BrownCircle",
                        type = "ELLIPSE",
                        bounds = new Bounds { x = 50f, y = 100f, width = 60f, height = 60f },
                        fills = new List<Fill>
                        {
                            new Fill { type = "SOLID", color = new ColorValue { r = 0.6f, g = 0.4f, b = 0.2f, a = 1f } }
                        }
                    },
                    new TextNode
                    {
                        id = "2:3",
                        name = "TitleText",
                        type = "TEXT",
                        characters = "Hello World",
                        fontSize = 18f,
                        textAlign = "CENTER",
                        bounds = new Bounds { x = 10f, y = 200f, width = 200f, height = 30f },
                        fills = new List<Fill>
                        {
                            new Fill { type = "SOLID", color = new ColorValue { r = 1f, g = 1f, b = 1f, a = 1f } }
                        }
                    }
                }
            };

            string uss = USSStyleGenerator.GenerateUSS(doc, "TestPackage");

            Assert.IsNotNull(uss);

            // Rectangle assertions
            Assert.Contains(".greenrect-2_1", uss);
            Assert.Contains("position: absolute;", uss);
            Assert.Contains("left: 10px;", uss);
            Assert.Contains("top: 20px;", uss);
            Assert.Contains("width: 120px;", uss);
            Assert.Contains("height: 80px;", uss);
            Assert.Contains("background-color: rgba(0, 255, 0, 1.00);", uss);
            Assert.Contains("border-radius: 4px;", uss);

            // Ellipse assertions
            Assert.Contains(".browncircle-2_2", uss);
            Assert.Contains("position: absolute;", uss);
            Assert.Contains("left: 50px;", uss);
            Assert.Contains("top: 100px;", uss);
            Assert.Contains("width: 60px;", uss);
            Assert.Contains("height: 60px;", uss);
            Assert.Contains("background-color: rgba(153, 102, 51, 1.00);", uss);
            Assert.Contains("border-radius: 50%;", uss);

            // Text assertions
            Assert.Contains(".titletext-2_3", uss);
            Assert.Contains("position: absolute;", uss);
            Assert.Contains("left: 10px;", uss);
            Assert.Contains("top: 200px;", uss);
            Assert.Contains("color: rgba(255, 255, 255, 1.00);", uss);
            Assert.Contains("background-color: transparent;", uss);
            Assert.Contains("font-size: 18px;", uss);
            Assert.Contains("-unity-text-align: middle-center;", uss);
        }

        [Test]
        public void TokenAssetGenerator_GenerateTokenAssets_CreatesTokenScriptableObjects()
        {
            var doc = new IRDocument
            {
                tokens = new Tokens
                {
                    colors = new List<ColorToken>
                    {
                        new ColorToken { id = "color-1", name = "PrimaryColor", value = new ColorValue { r = 0.2f, g = 0.4f, b = 0.8f, a = 1f }, hex = "#3366CC" }
                    },
                    typography = new List<TypographyToken>
                    {
                        new TypographyToken { id = "type-1", name = "HeadingLarge", fontFamily = "Inter", fontSize = 24f, fontWeight = 700f }
                    },
                    spacing = new List<SpacingToken>
                    {
                        new SpacingToken { id = "spacing-1", name = "SpaceMedium", value = 16f }
                    },
                    effects = new List<EffectToken>
                    {
                        new EffectToken { id = "effect-1", name = "DropShadowSoft", effects = new List<EffectValue>() }
                    }
                }
            };

            var assets = Figma2Unity.Editor.Importer.TokenAssetGenerator.GenerateTokenAssets(doc, null);

            Assert.IsNotNull(assets);
            Assert.IsNotNull(assets.ColorPaletteAsset);
            Assert.AreEqual(1, assets.ColorPaletteAsset.colors.Count);
            Assert.AreEqual("color-1", assets.ColorPaletteAsset.colors[0].id);
            Assert.IsTrue(assets.ColorPaletteAsset.TryGetToken("color-1", out var colorEntry));
            Assert.AreEqual("PrimaryColor", colorEntry.name);

            Assert.IsNotNull(assets.TypeRampAsset);
            Assert.AreEqual(1, assets.TypeRampAsset.typography.Count);
            Assert.AreEqual("HeadingLarge", assets.TypeRampAsset.typography[0].name);
            Assert.IsTrue(assets.TypeRampAsset.TryGetToken("type-1", out var typeEntry));
            Assert.AreEqual(24f, typeEntry.fontSize);

            Assert.IsNotNull(assets.SpacingScaleAsset);
            Assert.AreEqual(1, assets.SpacingScaleAsset.spacing.Count);
            Assert.AreEqual(16f, assets.SpacingScaleAsset.spacing[0].value);
            Assert.IsTrue(assets.SpacingScaleAsset.TryGetToken("spacing-1", out var spaceEntry));
            Assert.AreEqual(16f, spaceEntry.value);

            Assert.IsNotNull(assets.EffectStyleAsset);
            Assert.AreEqual(1, assets.EffectStyleAsset.effects.Count);
            Assert.IsTrue(assets.EffectStyleAsset.TryGetToken("effect-1", out var effectEntry));
            Assert.AreEqual("DropShadowSoft", effectEntry.name);
        }

        [Test]
        public void UIToolkitGenerator_GenerateUSS_OutputsDesignTokenVariablesAndRootBlock()
        {
            var doc = new IRDocument
            {
                tokens = new Tokens
                {
                    colors = new List<ColorToken>
                    {
                        new ColorToken { id = "token-blue", name = "Brand Blue", value = new ColorValue { r = 0f, g = 0.5f, b = 1f, a = 1f } }
                    },
                    typography = new List<TypographyToken>
                    {
                        new TypographyToken { id = "token-h1", name = "H1 Style", fontSize = 32f }
                    }
                },
                rootNodes = new List<IRNode>
                {
                    new RectangleNode
                    {
                        id = "3:1",
                        name = "TokenRect",
                        type = "RECTANGLE",
                        bounds = new Bounds { x = 0, y = 0, width = 100, height = 100 },
                        fills = new List<Fill>
                        {
                            new Fill { tokenId = "token-blue", type = "SOLID" }
                        }
                    },
                    new TextNode
                    {
                        id = "3:2",
                        name = "TokenText",
                        type = "TEXT",
                        characters = "Token Test",
                        typographyTokenId = "token-h1",
                        bounds = new Bounds { x = 0, y = 100, width = 200, height = 40 }
                    }
                }
            };

            string uss = UIToolkitGenerator.GenerateUSS(doc, "TokenPkg");

            Assert.IsNotNull(uss);
            Assert.Contains(":root {", uss);
            Assert.Contains("--color-brand-blue: rgba(0, 128, 255, 1.00);", uss);
            Assert.Contains("--font-size-h1-style: 32px;", uss);
            Assert.Contains("background-color: var(--color-brand-blue);", uss);
            Assert.Contains("font-size: var(--font-size-h1-style);", uss);
        }

        [Test]
        public void TMPFontMatcher_MatchOrGenerateFont_HandlesFontMatchingAndFallback()
        {
            var result = Figma2Unity.Editor.Fonts.TMPFontMatcher.MatchOrGenerateFont("NonExistentFontFamilyName123", 700f);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.UsedFallback || !result.Success);
            Assert.IsNotNull(result.LogMessage);
        }

        [Test]
        public void FontResolver_ResolveFontForTextNode_CollectsStructuredMissingFontReport()
        {
            Figma2Unity.Editor.Fonts.FontResolver.ClearReport();

            var textNode = new TextNode
            {
                id = "node-101",
                name = "SubtitleLabel",
                type = "TEXT",
                fontFamily = "UnknownFontFamilyCustom",
                fontWeight = 600f
            };

            var result = Figma2Unity.Editor.Fonts.FontResolver.ResolveFontForTextNode(textNode, null);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.UsedFallback || result.Success);
            Assert.Contains("[Figma2Unity] Missing Font: 'UnknownFontFamilyCustom' (600) on node 'SubtitleLabel' (ID: node-101)", result.LogMessage);

            var report = Figma2Unity.Editor.Fonts.FontResolver.MissingFontsReport;
            Assert.IsNotNull(report);
            Assert.AreEqual(1, report.Count);
            Assert.AreEqual("UnknownFontFamilyCustom", report[0].FontFamily);
            Assert.AreEqual(600f, report[0].FontWeight);
            Assert.AreEqual("SubtitleLabel", report[0].NodeName);
            Assert.AreEqual("node-101", report[0].NodeId);
        }

        [Test]
        public void SanitizeAssetPath_StripsSpecialCharactersAndSpaces()
        {
            string sanitized1 = UIToolkitGenerator.SanitizeAssetPath("exports/images/1_6@1x.png");
            Assert.AreEqual("exports/images/1_6_1x.png", sanitized1);

            string sanitized2 = UIToolkitGenerator.SanitizeAssetPath("exports/images/my image@2x.png");
            Assert.AreEqual("exports/images/my_image_2x.png", sanitized2);
        }

        [Test]
        public void TranslateUnityTextAlign_MapsAlignmentMatrixCorrectly()
        {
            Assert.AreEqual("upper-left", UIToolkitGenerator.TranslateUnityTextAlign("LEFT", "TOP"));
            Assert.AreEqual("middle-center", UIToolkitGenerator.TranslateUnityTextAlign("CENTER", "CENTER"));
            Assert.AreEqual("lower-right", UIToolkitGenerator.TranslateUnityTextAlign("RIGHT", "BOTTOM"));
            Assert.AreEqual("middle-left", UIToolkitGenerator.TranslateUnityTextAlign("JUSTIFY", "MIDDLE"));
        }

        [Test]
        public void USSStyleGenerator_TextDecorationAndFontDefinition_GeneratesCorrectUSSActions()
        {
            var doc = new IRDocument
            {
                rootNodes = new List<IRNode>
                {
                    new TextNode
                    {
                        id = "text-1",
                        name = "UnderlineText",
                        type = "TEXT",
                        characters = "Underlined Hello",
                        fontFamily = "Irish Grover",
                        textDecoration = "UNDERLINE",
                        textAlign = "CENTER",
                        textAlignVertical = "MIDDLE"
                    }
                }
            };

            string uss = USSStyleGenerator.GenerateUSS(doc, "TestPkg");

            Assert.IsFalse(uss.Contains("-unity-text-wrap: wrap;"));
            Assert.Contains("-unity-text-align: middle-center;", uss);
            Assert.Contains("white-space: normal;", uss);
            Assert.Contains("border-bottom-width: 1px;", uss);

            string uxml = UXMLTreeGenerator.GenerateUXML(doc.rootNodes[0], "style.uss");
            Assert.Contains("<u>Underlined Hello</u>", uxml);
        }

        [Test]
        public void TranslateCssProperty_InterceptsTextWrapToWhiteSpaceNormal()
        {
            Assert.AreEqual("white-space: normal;", UIToolkitGenerator.TranslateCssProperty("text-wrap", "wrap"));
            Assert.AreEqual("white-space: normal;", UIToolkitGenerator.TranslateCssProperty("-unity-text-wrap", "wrap"));
            Assert.AreEqual("white-space: nowrap;", UIToolkitGenerator.TranslateCssProperty("text-wrap", "nowrap"));
        }

        [Test]
        public void SaveAssetBytes_SanitizesFileNameBeforeSaving()
        {
            string tempDir = System.IO.Path.Combine(UnityEngine.Application.temporaryCachePath, "SanitizeSaveTest");
            string targetPath = System.IO.Path.Combine(tempDir, "exports/images/1_6@1x.png");
            byte[] data = new byte[] { 1, 2, 3, 4 };

            UIToolkitGenerator.SaveAssetBytes(targetPath, data);

            string expectedPath = System.IO.Path.Combine(tempDir, "exports/images/1_6_1x.png");
            Assert.IsTrue(System.IO.File.Exists(expectedPath));

            if (System.IO.File.Exists(expectedPath)) System.IO.File.Delete(expectedPath);
            if (System.IO.Directory.Exists(tempDir)) System.IO.Directory.Delete(tempDir, true);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Figma2Unity.Editor.Generator;
using Figma2Unity.Editor.Schema;
using Figma2Unity.Editor.Importer;
using Figma2Unity.Editor.Fonts;
using Figma2Unity.Tokens;

namespace Figma2Unity.Tests.Editor
{
    public class Phase6TokenTests
    {
        [Test]
        public void GenerateTokenAssets_IRDocumentWithTokens_CreatesScriptableObjects()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "Figma2UnityTokenTest_" + Guid.NewGuid().ToString("N"));

            var doc = new IRDocument
            {
                tokens = new Tokens
                {
                    colors = new List<ColorToken>
                    {
                        new ColorToken
                        {
                            id = "color-primary",
                            name = "Primary Color",
                            value = new ColorValue { r = 0.1f, g = 0.5f, b = 0.9f, a = 1.0f },
                            hex = "#1A80E6",
                            description = "Brand primary color"
                        }
                    },
                    typography = new List<TypographyToken>
                    {
                        new TypographyToken
                        {
                            id = "type-header",
                            name = "Header Text",
                            fontFamily = "Inter",
                            fontSize = 28f,
                            fontWeight = 700f
                        }
                    },
                    spacing = new List<SpacingToken>
                    {
                        new SpacingToken
                        {
                            id = "spacing-md",
                            name = "Medium Spacing",
                            value = 16f
                        }
                    },
                    effects = new List<EffectToken>
                    {
                        new EffectToken
                        {
                            id = "effect-shadow",
                            name = "Card Shadow",
                            effects = new List<EffectValue>()
                        }
                    }
                }
            };

            var assets = TokenAssetGenerator.GenerateTokenAssets(doc, tempDir);

            Assert.IsNotNull(assets);

            // 1. Verify ColorPaletteSO
            Assert.IsNotNull(assets.ColorPaletteAsset);
            Assert.AreEqual(1, assets.ColorPaletteAsset.colors.Count);
            Assert.AreEqual("color-primary", assets.ColorPaletteAsset.colors[0].id);
            Assert.AreEqual("Primary Color", assets.ColorPaletteAsset.colors[0].name);
            Assert.IsTrue(assets.ColorPaletteAsset.TryGetToken("color-primary", out var colorEntry));
            Assert.AreEqual("#1A80E6", colorEntry.hex);

            // 2. Verify TypeRampSO
            Assert.IsNotNull(assets.TypeRampAsset);
            Assert.AreEqual(1, assets.TypeRampAsset.typography.Count);
            Assert.AreEqual("type-header", assets.TypeRampAsset.typography[0].id);
            Assert.IsTrue(assets.TypeRampAsset.TryGetToken("type-header", out var typeEntry));
            Assert.AreEqual(28f, typeEntry.fontSize);
            Assert.AreEqual("Inter", typeEntry.fontFamily);

            // 3. Verify SpacingScaleSO
            Assert.IsNotNull(assets.SpacingScaleAsset);
            Assert.AreEqual(1, assets.SpacingScaleAsset.spacing.Count);
            Assert.IsTrue(assets.SpacingScaleAsset.TryGetToken("spacing-md", out var spaceEntry));
            Assert.AreEqual(16f, spaceEntry.value);

            // 4. Verify EffectStyleSO
            Assert.IsNotNull(assets.EffectStyleAsset);
            Assert.AreEqual(1, assets.EffectStyleAsset.effects.Count);
            Assert.IsTrue(assets.EffectStyleAsset.TryGetToken("effect-shadow", out var effectEntry));
            Assert.AreEqual("Card Shadow", effectEntry.name);

            // Cleanup temp directory if created
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void UIToolkitGenerator_GenerateUSS_OutputsRootVariablesAndVarReferences()
        {
            var doc = new IRDocument
            {
                tokens = new Tokens
                {
                    colors = new List<ColorToken>
                    {
                        new ColorToken
                        {
                            id = "color-brand-blue",
                            name = "Brand Blue",
                            value = new ColorValue { r = 0.0f, g = 0.4f, b = 0.8f, a = 1.0f }
                        }
                    },
                    typography = new List<TypographyToken>
                    {
                        new TypographyToken
                        {
                            id = "type-title",
                            name = "Title Style",
                            fontSize = 32f
                        }
                    },
                    spacing = new List<SpacingToken>
                    {
                        new SpacingToken
                        {
                            id = "spacing-lg",
                            name = "Large Space",
                            value = 24f
                        }
                    }
                },
                rootNodes = new List<IRNode>
                {
                    new RectangleNode
                    {
                        id = "10:1",
                        name = "ColorBox",
                        type = "RECTANGLE",
                        bounds = new Bounds { x = 0, y = 0, width = 100, height = 100 },
                        fills = new List<Fill>
                        {
                            new Fill { tokenId = "color-brand-blue", type = "SOLID" }
                        }
                    },
                    new TextNode
                    {
                        id = "10:2",
                        name = "TitleLabel",
                        type = "TEXT",
                        characters = "Design System Title",
                        typographyTokenId = "type-title",
                        bounds = new Bounds { x = 0, y = 110, width = 200, height = 40 }
                    }
                }
            };

            string uss = UIToolkitGenerator.GenerateUSS(doc, "TestPkg");

            Assert.IsNotNull(uss);

            // 1. Check :root block
            Assert.Contains(":root {", uss);
            Assert.Contains("--color-brand-blue: rgba(0, 102, 204, 1.00);", uss);
            Assert.Contains("--spacing-large-space: 24px;", uss);
            Assert.Contains("--font-size-title-style: 32px;", uss);

            // 2. Check var(--...) property usages
            Assert.Contains("background-color: var(--color-brand-blue);", uss);
            Assert.Contains("font-size: var(--font-size-title-style);", uss);
        }

        [Test]
        public void FontResolver_MissingFont_FlagsWarningEntryAndAppliesDefaultFallbackWithoutException()
        {
            FontResolver.ClearReport();

            var textNode = new TextNode
            {
                id = "node-99",
                name = "CustomHeader",
                type = "TEXT",
                fontFamily = "NonExistentFontFamilyXYZ",
                fontWeight = 700f
            };

            FontResolver.FontResolutionResult result = null;

            Assert.DoesNotThrow(() =>
            {
                result = FontResolver.ResolveFontForTextNode(textNode, null);
            });

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.UsedFallback);

            // Check structured warning log entry
            Assert.Contains("[Figma2Unity] Missing Font: 'NonExistentFontFamilyXYZ' (700) on node 'CustomHeader' (ID: node-99)", result.LogMessage);

            // Check missing font entry collected in report
            var report = FontResolver.MissingFontsReport;
            Assert.IsNotNull(report);
            Assert.AreEqual(1, report.Count);

            var entry = report[0];
            Assert.AreEqual("NonExistentFontFamilyXYZ", entry.FontFamily);
            Assert.AreEqual(700f, entry.FontWeight);
            Assert.AreEqual("CustomHeader", entry.NodeName);
            Assert.AreEqual("node-99", entry.NodeId);
        }
    }
}

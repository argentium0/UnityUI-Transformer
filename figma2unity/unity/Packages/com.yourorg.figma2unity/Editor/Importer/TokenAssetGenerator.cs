using System;
using System.IO;
using UnityEngine;
using Figma2Unity.Editor.Schema;
using Figma2Unity.Tokens;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Figma2Unity.Editor.Importer
{
    public static class TokenAssetGenerator
    {
        public class GeneratedTokenAssets
        {
            public ColorPaletteSO ColorPaletteAsset;
            public TypeRampSO TypeRampAsset;
            public SpacingScaleSO SpacingScaleAsset;
            public EffectStyleSO EffectStyleAsset;
        }

        public static GeneratedTokenAssets GenerateTokenAssets(IRDocument document, string destinationFolder)
        {
            if (document == null || document.tokens == null)
            {
                return null;
            }

            var result = new GeneratedTokenAssets();

            // 1. ColorPaletteSO
            var palette = ScriptableObject.CreateInstance<ColorPaletteSO>();
            if (document.tokens.colors != null)
            {
                foreach (var cToken in document.tokens.colors)
                {
                    palette.colors.Add(new ColorTokenEntry
                    {
                        id = cToken.id,
                        name = cToken.name,
                        color = cToken.value != null ? cToken.value.ToUnityColor() : Color.white,
                        hex = cToken.hex,
                        description = cToken.description
                    });
                }
            }
            result.ColorPaletteAsset = palette;

            // 2. TypeRampSO
            var typeRamp = ScriptableObject.CreateInstance<TypeRampSO>();
            if (document.tokens.typography != null)
            {
                foreach (var tToken in document.tokens.typography)
                {
                    typeRamp.typography.Add(new TypographyTokenEntry
                    {
                        id = tToken.id,
                        name = tToken.name,
                        fontFamily = tToken.fontFamily,
                        fontSize = tToken.fontSize,
                        fontWeight = tToken.fontWeight,
                        lineHeight = tToken.lineHeight,
                        letterSpacing = tToken.letterSpacing,
                        textCase = tToken.textCase,
                        textDecoration = tToken.textDecoration
                    });
                }
            }
            result.TypeRampAsset = typeRamp;

            // 3. SpacingScaleSO
            var spacingScale = ScriptableObject.CreateInstance<SpacingScaleSO>();
            if (document.tokens.spacing != null)
            {
                foreach (var sToken in document.tokens.spacing)
                {
                    spacingScale.spacing.Add(new SpacingTokenEntry
                    {
                        id = sToken.id,
                        name = sToken.name,
                        value = sToken.value
                    });
                }
            }
            result.SpacingScaleAsset = spacingScale;

            // 4. EffectStyleSO
            var effectStyle = ScriptableObject.CreateInstance<EffectStyleSO>();
            if (document.tokens.effects != null)
            {
                foreach (var eToken in document.tokens.effects)
                {
                    var entry = new EffectTokenEntry
                    {
                        id = eToken.id,
                        name = eToken.name
                    };

                    if (eToken.effects != null)
                    {
                        foreach (var eff in eToken.effects)
                        {
                            entry.effects.Add(new EffectTokenDataValue
                            {
                                type = eff.type,
                                color = eff.color != null ? eff.color.ToUnityColor() : Color.black,
                                offset = eff.offset != null ? new Vector2(eff.offset.x, eff.offset.y) : Vector2.zero,
                                radius = eff.radius ?? 0f,
                                spread = eff.spread ?? 0f
                            });
                        }
                    }

                    effectStyle.effects.Add(entry);
                }
            }
            result.EffectStyleAsset = effectStyle;

#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(destinationFolder))
            {
                string tokensFolder = Path.Combine(destinationFolder, "Tokens");
                if (!Directory.Exists(tokensFolder))
                {
                    Directory.CreateDirectory(tokensFolder);
                }

                SaveAsset(palette, Path.Combine(tokensFolder, "ColorPalette.asset"));
                SaveAsset(typeRamp, Path.Combine(tokensFolder, "TypeRamp.asset"));
                SaveAsset(spacingScale, Path.Combine(tokensFolder, "SpacingScale.asset"));
                SaveAsset(effectStyle, Path.Combine(tokensFolder, "EffectStyle.asset"));

                AssetDatabase.SaveAssets();
            }
#endif

            return result;
        }

#if UNITY_EDITOR
        private static void SaveAsset(ScriptableObject asset, string path)
        {
            string unityPath = path.Replace('\\', '/');
            AssetDatabase.CreateAsset(asset, unityPath);
        }
#endif
    }
}

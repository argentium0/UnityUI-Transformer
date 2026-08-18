using System;
using UnityEngine;

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
            var res = FontResolver.ResolveFont(fontFamily, fontWeight ?? 400f, "Text", "0:0");
            return new FontMatchResult
            {
                Success = res.Success,
                WasGenerated = res.WasGenerated,
                UsedFallback = res.UsedFallback,
                FontAsset = res.FontAsset,
                AssetPath = res.AssetPath,
                LogMessage = res.LogMessage
            };
        }
    }
}

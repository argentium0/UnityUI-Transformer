#nullable enable
using System;
using UnityUITransformer.App.Services;

namespace Figma2Unity.Pipeline
{
    public static class UssIdentifiers
    {
        public static string ToUssClass(string? rawIdOrName)
        {
            if (string.IsNullOrWhiteSpace(rawIdOrName)) return "element";
            string clean = UxmlGenerator.SanitizeName(rawIdOrName);
            return UxmlGenerator.ToKebabCase(clean);
        }

        public static string ToSafeToken(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            return raw.Replace(" ", "");
        }
    }
}

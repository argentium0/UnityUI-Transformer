using System;
using System.Text.RegularExpressions;
using Figma2Unity.Editor.Schema;

namespace Figma2Unity.Editor.Generator
{
    public static class USSStyleGenerator
    {
        public static string GenerateUSS(IRDocument document, string packageName)
        {
            return UIToolkitGenerator.GenerateUSS(document, packageName);
        }

        public static string SanitizeClassName(string name, string id)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = "element";
            }

            string sanitized = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9_-]", "-");
            sanitized = Regex.Replace(sanitized, @"^-+", "");
            sanitized = Regex.Replace(sanitized, @"-+$", "");

            if (string.IsNullOrEmpty(sanitized) || char.IsDigit(sanitized[0]))
            {
                sanitized = "elem-" + sanitized;
            }

            string sanitizedId = Regex.Replace(id ?? "", @"[^a-zA-Z0-9_-]", "_");
            return $"{sanitized}-{sanitizedId}";
        }
    }
}

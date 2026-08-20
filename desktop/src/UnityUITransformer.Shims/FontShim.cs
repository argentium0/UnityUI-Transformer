namespace Figma2Unity.Editor.Fonts
{
    public class FontResolutionResult
    {
        public bool Success { get; set; }
        public bool UsedFallback { get; set; }
        public string? AssetPath { get; set; }
    }

    public static class FontResolver
    {
        public static FontResolutionResult? ResolveFontForTextNode(object? textNode)
        {
            return null;
        }
    }
}

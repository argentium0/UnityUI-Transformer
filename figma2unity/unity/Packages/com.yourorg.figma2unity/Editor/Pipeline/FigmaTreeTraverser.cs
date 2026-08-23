#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using UnityUITransformer.App.Models;

namespace Figma2Unity.Pipeline
{
    public static class FigmaTreeTraverser
    {
        private static string GetString(JsonElement element, string prop) =>
            element.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String
                ? val.GetString() ?? string.Empty
                : string.Empty;

        private static float GetFloat(JsonElement element, string prop) =>
            element.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number
                ? val.GetSingle()
                : 0f;

        private static float ParseStyleFloat(JsonElement element, string prop) =>
            element.TryGetProperty("style", out var styleEl) && styleEl.ValueKind == JsonValueKind.Object
            && styleEl.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number
                ? val.GetSingle() : 0f;

        private static string ParseStyleString(JsonElement element, string prop) =>
            element.TryGetProperty("style", out var styleEl) && styleEl.ValueKind == JsonValueKind.Object
            && styleEl.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String
                ? val.GetString() ?? string.Empty : string.Empty;

        public static FigmaNode TraverseNode(JsonElement element)
        {
            var node = new FigmaNode
            {
                Id = GetString(element, "id"),
                Name = GetString(element, "name"),
                Type = GetString(element, "type"),
                LayoutMode = GetString(element, "layoutMode") is { Length: > 0 } lm ? lm : "NONE",
                ItemSpacing = GetFloat(element, "itemSpacing"),
                PaddingLeft = GetFloat(element, "paddingLeft"),
                PaddingRight = GetFloat(element, "paddingRight"),
                PaddingTop = GetFloat(element, "paddingTop"),
                PaddingBottom = GetFloat(element, "paddingBottom"),
                PrimaryAxisAlignItems = GetString(element, "primaryAxisAlignItems") is { Length: > 0 } p ? p : "MIN",
                CounterAxisAlignItems = GetString(element, "counterAxisAlignItems") is { Length: > 0 } c ? c : "MIN",
                LayoutGrow = element.TryGetProperty("layoutGrow", out var lg) && lg.ValueKind == JsonValueKind.Number ? lg.GetSingle() : (float?)null,
                Characters = GetString(element, "characters"),
                FontSize = ParseStyleFloat(element, "fontSize"),
                FontFamily = ParseStyleString(element, "fontFamily"),
                FontWeight = ParseStyleFloat(element, "fontWeight") is var fw && fw > 0 ? fw.ToString("0", System.Globalization.CultureInfo.InvariantCulture) : "400"
            };

            if (element.TryGetProperty("children", out var childrenEl) && childrenEl.ValueKind == JsonValueKind.Array)
            {
                node.Children = new List<FigmaNode>();
                foreach (var childEl in childrenEl.EnumerateArray())
                {
                    node.Children.Add(TraverseNode(childEl));
                }
            }

            return node;
        }
    }
}

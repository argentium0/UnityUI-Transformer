using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnityUITransformer.App.Models
{
    public class FigmaColor
    {
        [JsonPropertyName("r")]
        public float? R { get; set; }

        [JsonPropertyName("g")]
        public float? G { get; set; }

        [JsonPropertyName("b")]
        public float? B { get; set; }

        [JsonPropertyName("a")]
        public float? A { get; set; }

        public string ToRgbaString(float opacity = 1.0f)
        {
            int r = Math.Clamp((int)Math.Round((R ?? 0f) * 255f), 0, 255);
            int g = Math.Clamp((int)Math.Round((G ?? 0f) * 255f), 0, 255);
            int b = Math.Clamp((int)Math.Round((B ?? 0f) * 255f), 0, 255);
            float alpha = Math.Clamp((A ?? 1.0f) * opacity, 0f, 1f);
            return $"rgba({r}, {g}, {b}, {alpha:0.##})";
        }
    }

    public class FigmaPaint
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("visible")]
        public bool? Visible { get; set; }

        [JsonPropertyName("color")]
        public FigmaColor? Color { get; set; }

        [JsonPropertyName("opacity")]
        public float? Opacity { get; set; }

        [JsonPropertyName("imageRef")]
        public string? ImageRef { get; set; }
    }

    public class FigmaBoundingBox
    {
        [JsonPropertyName("x")]
        public float? X { get; set; }

        [JsonPropertyName("y")]
        public float? Y { get; set; }

        [JsonPropertyName("width")]
        public float? Width { get; set; }

        [JsonPropertyName("height")]
        public float? Height { get; set; }
    }

    public class FigmaTypeStyle
    {
        [JsonPropertyName("fontFamily")]
        public string? FontFamily { get; set; }

        [JsonPropertyName("fontPostScriptName")]
        public string? FontPostScriptName { get; set; }

        [JsonPropertyName("fontSize")]
        public float? FontSize { get; set; }

        [JsonPropertyName("fontWeight")]
        public float? FontWeight { get; set; }

        [JsonPropertyName("textAlignHorizontal")]
        public string? TextAlignHorizontal { get; set; }
    }

    public class FigmaUserProfile
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("handle")]
        public string? Handle { get; set; }

        [JsonPropertyName("img_url")]
        public string? ImgUrl { get; set; }
    }

    public class FigmaNodeResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("nodes")]
        public Dictionary<string, FigmaNodeContainer>? Nodes { get; set; }

        [JsonPropertyName("document")]
        public FigmaNode? Document { get; set; }

        public FigmaNode? GetRootNode()
        {
            if (Nodes != null && Nodes.Count > 0)
            {
                foreach (var pair in Nodes.Values)
                {
                    if (pair.Document != null) return pair.Document;
                }
            }
            return Document;
        }
    }

    public class FigmaNodeContainer
    {
        [JsonPropertyName("document")]
        public FigmaNode? Document { get; set; }
    }

    public class FigmaNode
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("characters")]
        public string? Characters { get; set; }

        [JsonPropertyName("children")]
        public List<FigmaNode>? Children { get; set; }

        [JsonPropertyName("fills")]
        public List<FigmaPaint>? Fills { get; set; }

        [JsonPropertyName("strokes")]
        public List<FigmaPaint>? Strokes { get; set; }

        [JsonPropertyName("strokeWeight")]
        public float? StrokeWeight { get; set; }

        [JsonPropertyName("absoluteBoundingBox")]
        public FigmaBoundingBox? AbsoluteBoundingBox { get; set; }

        private string _fontFamily = string.Empty;

        [JsonPropertyName("fontFamily")]
        public string FontFamily
        {
            get => !string.IsNullOrEmpty(_fontFamily) ? _fontFamily : (Style?.FontFamily ?? string.Empty);
            set => _fontFamily = value;
        }

        private float? _fontSize;

        [JsonPropertyName("fontSize")]
        public float FontSize
        {
            get => _fontSize.HasValue && _fontSize.Value > 0 ? _fontSize.Value : (Style?.FontSize ?? 0f);
            set => _fontSize = value;
        }

        [JsonPropertyName("style")]
        public FigmaTypeStyle? Style { get; set; }

        [JsonPropertyName("layoutMode")]
        public string? LayoutMode { get; set; }

        [JsonPropertyName("primaryAxisAlignItems")]
        public string? PrimaryAxisAlignItems { get; set; }

        [JsonPropertyName("counterAxisAlignItems")]
        public string? CounterAxisAlignItems { get; set; }

        [JsonPropertyName("paddingLeft")]
        public float? PaddingLeft { get; set; }

        [JsonPropertyName("paddingRight")]
        public float? PaddingRight { get; set; }

        [JsonPropertyName("paddingTop")]
        public float? PaddingTop { get; set; }

        [JsonPropertyName("paddingBottom")]
        public float? PaddingBottom { get; set; }

        [JsonPropertyName("itemSpacing")]
        public float? ItemSpacing { get; set; }

        [JsonPropertyName("cornerRadius")]
        public float? CornerRadius { get; set; }
    }
}

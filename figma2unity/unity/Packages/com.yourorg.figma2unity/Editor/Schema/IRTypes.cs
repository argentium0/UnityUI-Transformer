using System;
using System.Collections.Generic;
using UnityEngine;

namespace Figma2Unity.Editor.Schema
{
    [Serializable]
    public class ColorValue
    {
        public float r;
        public float g;
        public float b;
        public float a = 1f;

        public Color ToUnityColor()
        {
            return new Color(r, g, b, a);
        }
    }

    [Serializable]
    public class Bounds
    {
        public float x;
        public float y;
        public float width;
        public float height;
    }

    [Serializable]
    public class CornerRadius
    {
        public float topLeft;
        public float topRight;
        public float bottomRight;
        public float bottomLeft;
    }

    [Serializable]
    public class Padding
    {
        public float top;
        public float right;
        public float bottom;
        public float left;
    }

    [Serializable]
    public class AutoLayout
    {
        public string layoutMode = "NONE"; // NONE, HORIZONTAL, VERTICAL
        public float gap;
        public Padding padding = new Padding();
        public string primaryAxisSizingMode = "FIXED"; // FIXED, AUTO
        public string counterAxisSizingMode = "FIXED"; // FIXED, AUTO
        public string primaryAxisAlign = "MIN"; // MIN, CENTER, MAX, SPACE_BETWEEN
        public string counterAxisAlign = "MIN"; // MIN, CENTER, MAX, BASELINE
        public string layoutAlign = "INHERIT"; // STRETCH, INHERIT
        public float layoutGrow;
    }

    [Serializable]
    public class Constraints
    {
        public string horizontal = "MIN"; // MIN, CENTER, MAX, STRETCH, SCALE
        public string vertical = "MIN";
    }

    [Serializable]
    public class Fill
    {
        public string tokenId;
        public string type = "SOLID"; // SOLID, GRADIENT, IMAGE
        public ColorValue color;
        public float? opacity;
    }

    [Serializable]
    public class Stroke
    {
        public string tokenId;
        public ColorValue color;
        public float weight = 1f;
        public string align = "INSIDE"; // INSIDE, OUTSIDE, CENTER
        public List<float> dashPattern;
    }

    [Serializable]
    public class Vector2Offset
    {
        public float x;
        public float y;
    }

    [Serializable]
    public class EffectValue
    {
        public string type; // DROP_SHADOW, INNER_SHADOW, LAYER_BLUR, BACKGROUND_BLUR
        public ColorValue color;
        public Vector2Offset offset;
        public float? radius;
        public float? spread;
    }

    [Serializable]
    public class ColorToken
    {
        public string id;
        public string name;
        public ColorValue value;
        public string hex;
        public string description;
    }

    [Serializable]
    public class TypographyToken
    {
        public string id;
        public string name;
        public string fontFamily;
        public float fontSize;
        public float fontWeight = 400f;
        public float? lineHeight;
        public float? letterSpacing;
        public string textCase;
        public string textDecoration;
    }

    [Serializable]
    public class SpacingToken
    {
        public string id;
        public string name;
        public float value;
    }

    [Serializable]
    public class EffectToken
    {
        public string id;
        public string name;
        public List<EffectValue> effects = new List<EffectValue>();
    }

    [Serializable]
    public class Tokens
    {
        public List<ColorToken> colors = new List<ColorToken>();
        public List<TypographyToken> typography = new List<TypographyToken>();
        public List<SpacingToken> spacing = new List<SpacingToken>();
        public List<EffectToken> effects = new List<EffectToken>();
    }

    [Serializable]
    public class Metadata
    {
        public string exportedAt;
        public string figmaFileKey;
        public string figmaFileName;
        public string generatorVersion = "1.0.0";
    }

    [Serializable]
    public class IRDocument
    {
        public string schemaVersion = "1.0.0";
        public Metadata metadata = new Metadata();
        public Tokens tokens = new Tokens();
        public List<IRNode> rootNodes = new List<IRNode>();
    }
}

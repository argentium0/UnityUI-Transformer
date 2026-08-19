using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Figma2Unity.Editor.Schema
{
    [Serializable]
    [JsonConverter(typeof(IRNodeConverter))]
    public abstract class IRNode
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public bool visible { get; set; } = true;
        public float opacity { get; set; } = 1f;
        public float rotation { get; set; }
        public Bounds bounds { get; set; }
        public AutoLayout autoLayout { get; set; }
        public string layoutPositioning { get; set; } = "AUTO";
        public string layoutAlign { get; set; } = "INHERIT";
        public Constraints constraints { get; set; }
        public List<Fill> fills { get; set; } = new List<Fill>();
        public List<Stroke> strokes { get; set; } = new List<Stroke>();
        public CornerRadius cornerRadius { get; set; } = new CornerRadius();
        public List<EffectValue> effects { get; set; } = new List<EffectValue>();
    }

    [Serializable]
    public class FrameNode : IRNode
    {
        public List<IRNode> children { get; set; } = new List<IRNode>();
        public bool clipsContent { get; set; }
        public string imageAssetRef { get; set; }
    }

    [Serializable]
    public class GroupNode : IRNode
    {
        public List<IRNode> children { get; set; } = new List<IRNode>();
    }

    [Serializable]
    public class RectangleNode : IRNode { }

    [Serializable]
    public class EllipseNode : IRNode { }

    [Serializable]
    public class VectorNode : IRNode
    {
        public string svgAssetRef { get; set; }
        public string svgPathData { get; set; }
    }

    [Serializable]
    public class TextNode : IRNode
    {
        public string characters { get; set; }
        public string typographyTokenId { get; set; }
        public string fontFamily { get; set; }
        public float? fontSize { get; set; }
        public float? fontWeight { get; set; }
        public string textAlign { get; set; } = "LEFT";
        public string textAutoResize { get; set; } = "NONE";
    }

    [Serializable]
    public class ImageNode : IRNode
    {
        public string imageAssetRef { get; set; }
        public string scaleMode { get; set; } = "FILL";
    }

    [Serializable]
    public class ComponentInstanceNode : IRNode
    {
        public string componentId { get; set; }
        public Dictionary<string, string> variantProperties { get; set; }
        public List<IRNode> children { get; set; } = new List<IRNode>();
    }

    [Serializable]
    public class UnsupportedNode : IRNode
    {
        public string figmaNodeType { get; set; }
        public List<IRNode> children { get; set; } = new List<IRNode>();
    }
}

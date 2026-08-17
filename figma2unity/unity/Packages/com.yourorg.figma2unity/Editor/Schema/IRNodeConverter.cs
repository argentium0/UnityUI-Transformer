using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Figma2Unity.Editor.Schema
{
    public class IRNodeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IRNode).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            JObject jsonObject = JObject.Load(reader);
            string nodeType = jsonObject["type"]?.Value<string>();

            IRNode targetNode = CreateNodeInstance(nodeType);
            serializer.Populate(jsonObject.CreateReader(), targetNode);
            return targetNode;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        private IRNode CreateNodeInstance(string nodeType)
        {
            switch (nodeType)
            {
                case "FRAME":
                    return new FrameNode();
                case "GROUP":
                    return new GroupNode();
                case "RECTANGLE":
                    return new RectangleNode();
                case "ELLIPSE":
                    return new EllipseNode();
                case "VECTOR":
                    return new VectorNode();
                case "TEXT":
                    return new TextNode();
                case "IMAGE":
                    return new ImageNode();
                case "COMPONENT_INSTANCE":
                    return new ComponentInstanceNode();
                case "UNSUPPORTED":
                default:
                    return new UnsupportedNode();
            }
        }
    }
}

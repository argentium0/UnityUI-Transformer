using System;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityUITransformer.App.Models;

namespace UnityUITransformer.App.Services
{
    public class UxmlGenerator
    {
        private static readonly XNamespace UiNs = "UnityEngine.UIElements";
        private static readonly XNamespace UieNs = "UnityEditor.UIElements";

        public string GenerateUxml(FigmaNode rootNode, string? ussFileName = null)
        {
            if (rootNode == null) throw new ArgumentNullException(nameof(rootNode));

            var uxmlRoot = new XElement(UiNs + "UXML",
                new XAttribute(XNamespace.Xmlns + "ui", UiNs.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "uie", UieNs.NamespaceName)
            );

            // Inject USS stylesheet link tag if provided
            if (!string.IsNullOrWhiteSpace(ussFileName))
            {
                if (!ussFileName.EndsWith(".uss", StringComparison.OrdinalIgnoreCase))
                {
                    ussFileName += ".uss";
                }
                uxmlRoot.Add(new XElement(UiNs + "Style", new XAttribute("src", ussFileName)));
            }

            var rootElement = ConvertNodeToXElement(rootNode);
            if (rootElement != null)
            {
                uxmlRoot.Add(rootElement);
            }

            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                uxmlRoot
            );

            return doc.ToString();
        }

        private XElement ConvertNodeToXElement(FigmaNode node)
        {
            string sanitizedName = SanitizeName(node.Name ?? "Element");
            string className = ToKebabCase(sanitizedName);

            XElement element;

            string nodeTypeUpper = node.Type?.ToUpperInvariant() ?? "FRAME";
            switch (nodeTypeUpper)
            {
                case "TEXT":
                    string textValue = !string.IsNullOrWhiteSpace(node.Characters)
                        ? node.Characters
                        : (node.Name ?? "Text");
                    element = new XElement(UiNs + "Label",
                        new XAttribute("name", sanitizedName),
                        new XAttribute("text", textValue),
                        new XAttribute("class", className)
                    );
                    break;

                case "RECTANGLE":
                case "ELLIPSE":
                case "VECTOR":
                case "IMAGE":
                case "FRAME":
                case "GROUP":
                case "COMPONENT":
                case "INSTANCE":
                default:
                    element = new XElement(UiNs + "VisualElement",
                        new XAttribute("name", sanitizedName),
                        new XAttribute("class", className)
                    );
                    break;
            }

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    var childElement = ConvertNodeToXElement(child);
                    if (childElement != null)
                    {
                        element.Add(childElement);
                    }
                }
            }

            return element;
        }

        public static string SanitizeName(string? rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return "Element";
            string name = rawName.Trim();
            if (name.EndsWith(".uxml", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".uss", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - 5);
            }
            string clean = Regex.Replace(name, @"[^\w\-]", "");
            return string.IsNullOrEmpty(clean) ? "Element" : clean;
        }

        public static string SanitizeNodeIdForFileName(string? rawId)
        {
            if (string.IsNullOrWhiteSpace(rawId)) return "1_0";
            return rawId.Replace(":", "_").Replace(";", "_");
        }

        public static string GetHashedImageFileName(string? rawId)
        {
            if (string.IsNullOrWhiteSpace(rawId)) return "img_0.png";
            int hash = Math.Abs(rawId.GetHashCode());
            return $"img_{hash}.png";
        }

        public static string ToKebabCase(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "element";
            string kebab = Regex.Replace(name, @"([a-z0-9])([A-Z])", "$1-$2").ToLowerInvariant();
            kebab = Regex.Replace(kebab, @"[\s_]+", "-");
            return kebab;
        }
    }
}

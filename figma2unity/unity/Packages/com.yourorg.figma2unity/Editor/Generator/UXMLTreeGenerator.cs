using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Figma2Unity.Editor.Schema;

namespace Figma2Unity.Editor.Generator
{
    public static class UXMLTreeGenerator
    {
        private static readonly XNamespace UiNs = "UnityEngine.UIElements";
        private static readonly XNamespace UieNs = "UnityEditor.UIElements";

        private class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => new UTF8Encoding(false);
        }

        public static string GenerateUXML(IRNode rootNode, string ussRelativePath)
        {
            if (rootNode == null) return string.Empty;

            var doc = new XDocument();
            var rootUxml = new XElement(UiNs + "UXML",
                new XAttribute(XNamespace.Xmlns + "ui", UiNs.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "uie", UieNs.NamespaceName)
            );

            // Add Style reference if USS path is provided
            if (!string.IsNullOrEmpty(ussRelativePath))
            {
                string sanitizedUssPath = ussRelativePath.Replace('\\', '/');
                rootUxml.Add(new XElement(UiNs + "Style", new XAttribute("src", sanitizedUssPath)));
            }

            // Generate tree hierarchy starting from rootNode
            XElement rootElement = BuildXmlElement(rootNode);
            if (rootElement != null)
            {
                rootUxml.Add(rootElement);
            }

            doc.Add(rootUxml);

            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
                OmitXmlDeclaration = true
            };

            using (var stringWriter = new Utf8StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
                {
                    doc.Save(xmlWriter);
                }
                return stringWriter.ToString();
            }
        }

        private static XElement BuildXmlElement(IRNode node)
        {
            if (node == null || !node.visible) return null;

            Figma2Unity.Editor.Reporting.FigmaImportLogger.LogNodeProcessed(node.type);

            string className = USSStyleGenerator.SanitizeClassName(node.name, node.id);

            XElement element;

            if (node is TextNode textNode)
            {
                element = new XElement(UiNs + "Label");
                element.SetAttributeValue("text", textNode.characters ?? string.Empty);
            }
            else if (node is ImageNode imageNode)
            {
                element = new XElement(UiNs + "Image");
                Figma2Unity.Editor.Reporting.FigmaImportLogger.LogRasterizedNode(node.id, node.name, node.type, "Image fill rasterization");
            }
            else if (node is VectorNode vectorNode)
            {
                element = new XElement(UiNs + "Image");
                Figma2Unity.Editor.Reporting.FigmaImportLogger.LogRasterizedNode(node.id, node.name, node.type, "Vector shape rasterization");
            }
            else if (node is UnsupportedNode unsupportedNode)
            {
                element = new XElement(UiNs + "Image");
                Figma2Unity.Editor.Reporting.FigmaImportLogger.LogRasterizedNode(node.id, node.name, node.type, "Unsupported node type fallback to rasterization");
            }
            else
            {
                element = new XElement(UiNs + "VisualElement");
            }

            element.SetAttributeValue("name", node.name ?? "Element");
            element.SetAttributeValue("class", className);

            // Add children for container node types
            if (node is FrameNode frameNode && frameNode.children != null)
            {
                foreach (var child in frameNode.children)
                {
                    var childXml = BuildXmlElement(child);
                    if (childXml != null) element.Add(childXml);
                }
            }
            else if (node is GroupNode groupNode && groupNode.children != null)
            {
                foreach (var child in groupNode.children)
                {
                    var childXml = BuildXmlElement(child);
                    if (childXml != null) element.Add(childXml);
                }
            }
            else if (node is ComponentInstanceNode compNode && compNode.children != null)
            {
                foreach (var child in compNode.children)
                {
                    var childXml = BuildXmlElement(child);
                    if (childXml != null) element.Add(childXml);
                }
            }

            return element;
        }
    }
}

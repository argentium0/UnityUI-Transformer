#nullable enable
using System;
using System.Collections.Generic;
using System.Security;
using System.Text;
using UnityUITransformer.App.Models;

namespace Figma2Unity.Pipeline
{
    public class UxmlDocumentBuilder
    {
        private readonly StringBuilder _uxmlBuilder = new();
        private readonly UssStyleBuilder _ussBuilder = new();

        public (string uxml, string uss) Build(FigmaNode rootNode)
        {
            _uxmlBuilder.AppendLine("<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" xmlns:uie=\"UnityEditor.UIElements\">");
            AppendNode(rootNode, parentHasAutoLayout: false);
            _uxmlBuilder.AppendLine("</ui:UXML>");

            return (_uxmlBuilder.ToString(), _ussBuilder.Build());
        }

        private void AppendNode(FigmaNode node, bool parentHasAutoLayout)
        {
            _ussBuilder.AppendLayoutRule(node, parentHasAutoLayout);
            _ussBuilder.AppendTypographyRule(node);

            var (tagName, extraAttrs) = MapNodeToElement(node);
            string className = UssIdentifiers.ToUssClass(!string.IsNullOrEmpty(node.Name) ? node.Name : node.Id);

            _uxmlBuilder.AppendLine($"  <ui:{tagName} name=\"{SecurityElement.Escape(node.Name ?? "Element")}\" class=\"{className}\"{extraAttrs}>");

            var childParentHasAutoLayout = node.LayoutMode is "HORIZONTAL" or "VERTICAL";

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    AppendNode(child, childParentHasAutoLayout);
                }
            }

            _uxmlBuilder.AppendLine($"  </ui:{tagName}>");
        }

        public static (string tagName, string extraAttrs) MapNodeToElement(FigmaNode node)
        {
            return node.Type?.ToUpperInvariant() switch
            {
                "TEXT" => ("Label", $" text=\"{SecurityElement.Escape(string.IsNullOrWhiteSpace(node.Characters) ? (node.Name ?? "") : node.Characters)}\""),
                _ => ("VisualElement", string.Empty)
            };
        }
    }
}

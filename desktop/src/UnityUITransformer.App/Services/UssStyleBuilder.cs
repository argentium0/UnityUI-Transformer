#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityUITransformer.App.Models;

namespace Figma2Unity.Pipeline
{
    public class UssStyleBuilder
    {
        private readonly StringBuilder _sb = new();

        public string Build() => _sb.ToString();

        private static string Px(float? val) =>
            val.HasValue ? $"{val.Value.ToString("0.##", CultureInfo.InvariantCulture)}px" : "0px";

        private static string Px(float val) =>
            $"{val.ToString("0.##", CultureInfo.InvariantCulture)}px";

        public void AppendLayoutRule(FigmaNode node, bool parentHasAutoLayout)
        {
            string className = UssIdentifiers.ToUssClass(!string.IsNullOrEmpty(node.Name) ? node.Name : node.Id);
            _sb.AppendLine($".{className} {{");

            if (parentHasAutoLayout)
            {
                _sb.AppendLine("    position: relative;");
                if (node.LayoutGrow is > 0f)
                    _sb.AppendLine($"    flex-grow: {node.LayoutGrow.Value.ToString("0.##", CultureInfo.InvariantCulture)};");
                else if (node.AbsoluteBoundingBox is { } selfBox)
                {
                    _sb.AppendLine($"    width: {Px(selfBox.Width)};");
                    _sb.AppendLine($"    height: {Px(selfBox.Height)};");
                }
            }
            else if (node.AbsoluteBoundingBox is { } box)
            {
                _sb.AppendLine("    position: absolute;");
                _sb.AppendLine($"    left: {Px(box.X)};");
                _sb.AppendLine($"    top: {Px(box.Y)};");
                _sb.AppendLine($"    width: {Px(box.Width)};");
                _sb.AppendLine($"    height: {Px(box.Height)};");
            }

            if (node.LayoutMode is "HORIZONTAL" or "VERTICAL")
            {
                _sb.AppendLine("    display: flex;");
                _sb.AppendLine($"    flex-direction: {(node.LayoutMode == "HORIZONTAL" ? "row" : "column")};");
                _sb.AppendLine($"    padding-left: {Px(node.PaddingLeft)};");
                _sb.AppendLine($"    padding-right: {Px(node.PaddingRight)};");
                _sb.AppendLine($"    padding-top: {Px(node.PaddingTop)};");
                _sb.AppendLine($"    padding-bottom: {Px(node.PaddingBottom)};");
                _sb.AppendLine($"    justify-content: {MapPrimaryAxisAlign(node.PrimaryAxisAlignItems)};");
                _sb.AppendLine($"    align-items: {MapCounterAxisAlign(node.CounterAxisAlignItems)};");
            }
            _sb.AppendLine("}");
            _sb.AppendLine();
        }

        private static string MapPrimaryAxisAlign(string v) => v switch { "CENTER" => "center", "MAX" => "flex-end", "SPACE_BETWEEN" => "space-between", _ => "flex-start" };
        private static string MapCounterAxisAlign(string v) => v switch { "CENTER" => "center", "MAX" => "flex-end", _ => "flex-start" };

        public void AppendTypographyRule(FigmaNode node)
        {
            if (node.Type != "TEXT") return;
            string className = UssIdentifiers.ToUssClass(!string.IsNullOrEmpty(node.Name) ? node.Name : node.Id);
            _sb.AppendLine($".{className} {{");
            if (node.FontSize > 0f) _sb.AppendLine($"    font-size: {Px(node.FontSize)};");
            if (int.TryParse(node.FontWeight, out var weight)) _sb.AppendLine($"    -unity-font-style: {(weight >= 700 ? "bold" : "normal")};");

            if (!string.IsNullOrEmpty(node.FontFamily))
            {
                var fontAssetPath = $"project://database/Assets/Fonts/{UssIdentifiers.ToSafeToken(node.FontFamily)}.asset";
                _sb.AppendLine($"    -unity-font-definition: url('{fontAssetPath}');");
            }
            _sb.AppendLine("}");
            _sb.AppendLine();
        }

        public static void AppendLayoutRule(FigmaNode node, List<string> rules)
        {
            if (node == null || rules == null) return;

            if (node.AbsoluteBoundingBox != null)
            {
                if (node.AbsoluteBoundingBox.Width.HasValue && node.AbsoluteBoundingBox.Width.Value > 0)
                {
                    rules.Add($"width: {node.AbsoluteBoundingBox.Width.Value.ToString("0.##", CultureInfo.InvariantCulture)}px;");
                }
                if (node.AbsoluteBoundingBox.Height.HasValue && node.AbsoluteBoundingBox.Height.Value > 0)
                {
                    rules.Add($"height: {node.AbsoluteBoundingBox.Height.Value.ToString("0.##", CultureInfo.InvariantCulture)}px;");
                }
            }
        }

        public static void AppendTypographyRule(FigmaNode node, List<string> rules)
        {
            if (node == null || rules == null) return;

            bool isText = string.Equals(node.Type, "TEXT", StringComparison.OrdinalIgnoreCase);
            if (isText)
            {
                if (node.FontSize > 0)
                {
                    rules.Add($"font-size: {node.FontSize.ToString("0.##", CultureInfo.InvariantCulture)}px;");
                }

                string fontFamily = node.FontFamily;
                if (!string.IsNullOrWhiteSpace(fontFamily))
                {
                    string safeFontName = fontFamily.Replace(" ", "");
                    rules.Add($"-unity-font-definition: url('project://database/Assets/Fonts/{safeFontName}.asset');");
                }
            }
        }
    }
}

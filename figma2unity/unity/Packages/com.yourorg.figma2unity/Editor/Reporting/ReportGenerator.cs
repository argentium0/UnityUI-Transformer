using System;
using System.IO;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Figma2Unity.Editor.Reporting
{
    public static class ReportGenerator
    {
        public class ReportResult
        {
            public bool Success;
            public string MarkdownPath;
            public string HtmlPath;
        }

        public static ReportResult GenerateReports(ImportReportData reportData, string destinationFolder)
        {
            if (reportData == null)
            {
                return new ReportResult { Success = false };
            }

            if (string.IsNullOrEmpty(destinationFolder))
            {
                destinationFolder = Path.Combine("Assets", "Figma2Unity", "Generated", reportData.PackageName ?? "Package");
            }

            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            string mdContent = BuildMarkdownReport(reportData);
            string htmlContent = BuildHtmlReport(reportData);

            string mdPath = Path.Combine(destinationFolder, "ImportReport.md");
            string htmlPath = Path.Combine(destinationFolder, "ImportReport.html");

            var encoding = new UTF8Encoding(false);
            File.WriteAllText(mdPath, mdContent, encoding);
            File.WriteAllText(htmlPath, htmlContent, encoding);

            var result = new ReportResult
            {
                Success = true,
                MarkdownPath = mdPath.Replace('\\', '/'),
                HtmlPath = htmlPath.Replace('\\', '/')
            };

#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif

            return result;
        }

        public static string BuildMarkdownReport(ImportReportData report)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Figma2Unity Import Report: {report.PackageName}");
            sb.AppendLine();
            sb.AppendLine($"- **Imported At (UTC)**: {report.ImportedAt}");
            sb.AppendLine($"- **IR Schema Version**: {report.SchemaVersion}");
            sb.AppendLine($"- **Total Nodes Processed**: {report.TotalNodesProcessed}");
            sb.AppendLine();

            // 1. Node Breakdown
            sb.AppendLine("## Node Breakdown by Type");
            sb.AppendLine();
            if (report.NodeCountByType != null && report.NodeCountByType.Count > 0)
            {
                sb.AppendLine("| Node Type | Count |");
                sb.AppendLine("| --- | --- |");
                foreach (var kvp in report.NodeCountByType)
                {
                    sb.AppendLine($"| {kvp.Key} | {kvp.Value} |");
                }
            }
            else
            {
                sb.AppendLine("*No nodes processed.*");
            }
            sb.AppendLine();

            // 2. Rasterized Nodes
            sb.AppendLine($"## Rasterized Nodes ({report.RasterizedNodes.Count})");
            sb.AppendLine();
            if (report.RasterizedNodes.Count > 0)
            {
                sb.AppendLine("| Node ID | Node Name | Type | Reason |");
                sb.AppendLine("| --- | --- | --- | --- |");
                foreach (var node in report.RasterizedNodes)
                {
                    sb.AppendLine($"| {node.NodeId} | {node.NodeName} | {node.NodeType} | {node.Reason} |");
                }
            }
            else
            {
                sb.AppendLine("*No nodes fell back to rasterization.*");
            }
            sb.AppendLine();

            // 3. Missing Fonts
            sb.AppendLine($"## Missing Fonts ({report.MissingFonts.Count})");
            sb.AppendLine();
            if (report.MissingFonts.Count > 0)
            {
                sb.AppendLine("| Node ID | Node Name | Figma Font | Fallback Applied |");
                sb.AppendLine("| --- | --- | --- | --- |");
                foreach (var font in report.MissingFonts)
                {
                    sb.AppendLine($"| {font.NodeId} | {font.NodeName} | '{font.FontFamily}' ({font.FontWeight}) | {font.FallbackFont} |");
                }
            }
            else
            {
                sb.AppendLine("*All fonts successfully matched.*");
            }
            sb.AppendLine();

            // 4. Validation Warnings
            sb.AppendLine($"## Validation & Schema Warnings ({report.ValidationWarnings.Count})");
            sb.AppendLine();
            if (report.ValidationWarnings.Count > 0)
            {
                foreach (var warn in report.ValidationWarnings)
                {
                    sb.AppendLine($"- **[{warn.Category}]**: {warn.Message}");
                }
            }
            else
            {
                sb.AppendLine("*No validation or schema warnings.*");
            }
            sb.AppendLine();

            return sb.ToString();
        }

        public static string BuildHtmlReport(ImportReportData report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine($"  <title>Figma2Unity Report - {report.PackageName}</title>");
            sb.AppendLine("  <style>");
            sb.AppendLine("    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 30px; background: #1e1e1e; color: #d4d4d4; }");
            sb.AppendLine("    h1 { color: #569cd6; border-bottom: 2px solid #333; padding-bottom: 10px; }");
            sb.AppendLine("    h2 { color: #4ec9b0; margin-top: 30px; }");
            sb.AppendLine("    table { width: 100%; border-collapse: collapse; margin-top: 10px; background: #252526; }");
            sb.AppendLine("    th, td { border: 1px solid #3c3c3c; padding: 10px; text-align: left; }");
            sb.AppendLine("    th { background: #333333; color: #dcdcdc; }");
            sb.AppendLine("    ul { background: #252526; padding: 15px 30px; border: 1px solid #3c3c3c; border-radius: 4px; }");
            sb.AppendLine("    .summary-box { background: #2d2d2d; padding: 15px; border-radius: 6px; border-left: 4px solid #007acc; }");
            sb.AppendLine("  </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine($"  <h1>Figma2Unity Import Report: {report.PackageName}</h1>");
            sb.AppendLine("  <div class='summary-box'>");
            sb.AppendLine($"    <p><strong>Imported At (UTC):</strong> {report.ImportedAt}</p>");
            sb.AppendLine($"    <p><strong>IR Schema Version:</strong> {report.SchemaVersion}</p>");
            sb.AppendLine($"    <p><strong>Total Nodes Processed:</strong> {report.TotalNodesProcessed}</p>");
            sb.AppendLine("  </div>");

            // Node Breakdown
            sb.AppendLine($"  <h2>Node Breakdown by Type</h2>");
            if (report.NodeCountByType != null && report.NodeCountByType.Count > 0)
            {
                sb.AppendLine("  <table><tr><th>Node Type</th><th>Count</th></tr>");
                foreach (var kvp in report.NodeCountByType)
                {
                    sb.AppendLine($"    <tr><td>{kvp.Key}</td><td>{kvp.Value}</td></tr>");
                }
                sb.AppendLine("  </table>");
            }
            else
            {
                sb.AppendLine("  <p><em>No nodes processed.</em></p>");
            }

            // Rasterized Nodes
            sb.AppendLine($"  <h2>Rasterized Nodes ({report.RasterizedNodes.Count})</h2>");
            if (report.RasterizedNodes.Count > 0)
            {
                sb.AppendLine("  <table><tr><th>Node ID</th><th>Node Name</th><th>Type</th><th>Reason</th></tr>");
                foreach (var node in report.RasterizedNodes)
                {
                    sb.AppendLine($"    <tr><td>{node.NodeId}</td><td>{node.NodeName}</td><td>{node.NodeType}</td><td>{node.Reason}</td></tr>");
                }
                sb.AppendLine("  </table>");
            }
            else
            {
                sb.AppendLine("  <p><em>No nodes fell back to rasterization.</em></p>");
            }

            // Missing Fonts
            sb.AppendLine($"  <h2>Missing Fonts ({report.MissingFonts.Count})</h2>");
            if (report.MissingFonts.Count > 0)
            {
                sb.AppendLine("  <table><tr><th>Node ID</th><th>Node Name</th><th>Figma Font</th><th>Fallback Applied</th></tr>");
                foreach (var font in report.MissingFonts)
                {
                    sb.AppendLine($"    <tr><td>{font.NodeId}</td><td>{font.NodeName}</td><td>'{font.FontFamily}' ({font.FontWeight})</td><td>{font.FallbackFont}</td></tr>");
                }
                sb.AppendLine("  </table>");
            }
            else
            {
                sb.AppendLine("  <p><em>All fonts successfully matched.</em></p>");
            }

            // Validation Warnings
            sb.AppendLine($"  <h2>Validation & Schema Warnings ({report.ValidationWarnings.Count})</h2>");
            if (report.ValidationWarnings.Count > 0)
            {
                sb.AppendLine("  <ul>");
                foreach (var warn in report.ValidationWarnings)
                {
                    sb.AppendLine($"    <li><strong>[{warn.Category}]</strong>: {warn.Message}</li>");
                }
                sb.AppendLine("  </ul>");
            }
            else
            {
                sb.AppendLine("  <p><em>No validation or schema warnings.</em></p>");
            }

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}

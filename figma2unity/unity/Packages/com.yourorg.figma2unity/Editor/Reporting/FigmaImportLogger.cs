using System;
using System.Collections.Generic;

namespace Figma2Unity.Editor.Reporting
{
    [Serializable]
    public class RasterizedNodeEntry
    {
        public string NodeId;
        public string NodeName;
        public string NodeType;
        public string Reason;
    }

    [Serializable]
    public class MissingFontEntry
    {
        public string FontFamily;
        public float FontWeight;
        public string NodeName;
        public string NodeId;
        public string FallbackFont;
    }

    [Serializable]
    public class ValidationWarningEntry
    {
        public string Category;
        public string Message;
    }

    [Serializable]
    public class ImportReportData
    {
        public string PackageName;
        public string ImportedAt;
        public string SchemaVersion;
        public int TotalNodesProcessed;
        public Dictionary<string, int> NodeCountByType = new Dictionary<string, int>();

        public List<RasterizedNodeEntry> RasterizedNodes = new List<RasterizedNodeEntry>();
        public List<MissingFontEntry> MissingFonts = new List<MissingFontEntry>();
        public List<ValidationWarningEntry> ValidationWarnings = new List<ValidationWarningEntry>();
    }

    public static class FigmaImportLogger
    {
        public static ImportReportData CurrentReport { get; private set; }

        public static void BeginSession(string packageName, string schemaVersion)
        {
            CurrentReport = new ImportReportData
            {
                PackageName = packageName ?? "SyncPackage",
                ImportedAt = DateTime.UtcNow.ToString("o"),
                SchemaVersion = schemaVersion ?? "1.0.0"
            };
        }

        public static void LogNodeProcessed(string nodeType)
        {
            if (CurrentReport == null) return;
            CurrentReport.TotalNodesProcessed++;
            if (string.IsNullOrEmpty(nodeType)) nodeType = "UNKNOWN";

            if (CurrentReport.NodeCountByType.ContainsKey(nodeType))
            {
                CurrentReport.NodeCountByType[nodeType]++;
            }
            else
            {
                CurrentReport.NodeCountByType[nodeType] = 1;
            }
        }

        public static void LogRasterizedNode(string nodeId, string nodeName, string nodeType, string reason)
        {
            if (CurrentReport == null) return;
            CurrentReport.RasterizedNodes.Add(new RasterizedNodeEntry
            {
                NodeId = nodeId ?? "",
                NodeName = nodeName ?? "",
                NodeType = nodeType ?? "",
                Reason = reason ?? "Fallback rasterization"
            });
        }

        public static void LogMissingFont(string fontFamily, float fontWeight, string nodeName, string nodeId, string fallbackFont = "LiberationSans SDF")
        {
            if (CurrentReport == null) return;
            CurrentReport.MissingFonts.Add(new MissingFontEntry
            {
                FontFamily = fontFamily ?? "",
                FontWeight = fontWeight,
                NodeName = nodeName ?? "",
                NodeId = nodeId ?? "",
                FallbackFont = fallbackFont ?? "LiberationSans SDF"
            });
        }

        public static void LogValidationWarning(string category, string message)
        {
            if (CurrentReport == null) return;
            CurrentReport.ValidationWarnings.Add(new ValidationWarningEntry
            {
                Category = category ?? "General",
                Message = message ?? ""
            });
        }

        public static ImportReportData EndSession()
        {
            var report = CurrentReport;
            return report;
        }
    }
}

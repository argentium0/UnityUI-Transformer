using System;
using System.IO;
using System.Threading.Tasks;

namespace UnityUITransformer.App.Services
{
    public class ExportService
    {
        public async Task<string> ExportUxmlAsync(string uxmlContent, string targetDirectory, string fileName)
        {
            if (string.IsNullOrWhiteSpace(targetDirectory))
            {
                targetDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "UI", "Generated");
            }

            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "GeneratedLayout";
            }

            string safeFileName = UxmlGenerator.SanitizeName(fileName);
            if (!safeFileName.EndsWith(".uxml", StringComparison.OrdinalIgnoreCase))
            {
                safeFileName += ".uxml";
            }

            string fullPath = Path.Combine(targetDirectory, safeFileName);
            await File.WriteAllTextAsync(fullPath, uxmlContent);

            return fullPath;
        }

        public async Task<string> ExportUssAsync(string ussContent, string targetDirectory, string fileName)
        {
            if (string.IsNullOrWhiteSpace(targetDirectory))
            {
                targetDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "UI", "Generated");
            }

            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "GeneratedStyle";
            }

            string safeFileName = UxmlGenerator.SanitizeName(fileName);
            if (!safeFileName.EndsWith(".uss", StringComparison.OrdinalIgnoreCase))
            {
                safeFileName += ".uss";
            }

            string fullPath = Path.Combine(targetDirectory, safeFileName);
            await File.WriteAllTextAsync(fullPath, ussContent);

            return fullPath;
        }
    }
}

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

            if (!fileName.EndsWith(".uxml", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".uxml";
            }

            string fullPath = Path.Combine(targetDirectory, fileName);
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

            if (!fileName.EndsWith(".uss", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".uss";
            }

            string fullPath = Path.Combine(targetDirectory, fileName);
            await File.WriteAllTextAsync(fullPath, ussContent);

            return fullPath;
        }
    }
}

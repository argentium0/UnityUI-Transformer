using System;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Figma2Unity.Editor.Schema;
using Figma2Unity.Editor.Generator;
using Figma2Unity.Editor.Reporting;

namespace Figma2Unity.Editor.Importer
{
    public class FigmaAutoImportTrigger : AssetPostprocessor
    {
        private static bool _isProcessing = false;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (_isProcessing) return;

            foreach (string assetPath in importedAssets)
            {
                if (string.IsNullOrEmpty(assetPath)) continue;

                // Normalize path delimiters
                string normalizedPath = assetPath.Replace('\\', '/');

                // Detect incoming ir-document.json file saved by Fastify bridge server or dropped into FigmaImport folder
                if (normalizedPath.EndsWith("ir-document.json", StringComparison.OrdinalIgnoreCase) &&
                    (normalizedPath.Contains("FigmaImport") || normalizedPath.Contains("Figma2UnityImports")))
                {
                    _isProcessing = true;
                    try
                    {
                        ProcessIncomingIRDocument(normalizedPath);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Figma2Unity AutoImport] Failed to auto-import live sync document at '{assetPath}': {ex.Message}\n{ex.StackTrace}");
                    }
                    finally
                    {
                        _isProcessing = false;
                    }
                    break;
                }
            }
        }

        public static void ProcessIncomingIRDocument(string irJsonAssetPath)
        {
            string fullPath = Path.GetFullPath(irJsonAssetPath);
            if (!File.Exists(fullPath)) return;

            string jsonContent = File.ReadAllText(fullPath);
            IRDocument document = SyncPackageImporter.ParseIRDocument(jsonContent);
            if (document == null) return;

            // Extract package name from folder structure (e.g., Assets/Figma2UnityImports/MyPackage/ir-document.json)
            string directoryName = Path.GetFileName(Path.GetDirectoryName(fullPath));
            string packageName = !string.IsNullOrEmpty(directoryName) ? directoryName : "LiveSyncPackage";

            // 1. Initialize centralized logger session
            FigmaImportLogger.BeginSession(packageName, document.schemaVersion);

            string generatedFolder = Path.Combine("Assets", "Figma2Unity", "Generated", packageName);

            // 2. Automatically generate UI Toolkit UXML & USS stylesheets
            UIToolkitGenerator.Generate(document, generatedFolder, packageName);

            // 3. Automatically generate uGUI Canvas & RectTransform Prefabs
            UGUIGenerator.Generate(document, generatedFolder, packageName);

            // 4. Automatically generate Token ScriptableObject assets
            TokenAssetGenerator.GenerateTokenAssets(document, generatedFolder);

            // 5. Automatically match / generate TextMeshPro SDF font assets
            string fontsFolder = Path.Combine("Assets", "Figma2Unity", "Generated", "Fonts");
            SyncPackageImporter.ResolveTextNodeFonts(document, fontsFolder);

            // 6. Generate post-import Markdown & HTML reports
            var reportData = FigmaImportLogger.EndSession();
            ReportGenerator.GenerateReports(reportData, generatedFolder);

            Debug.Log($"[Figma2Unity AutoImport] Live hot-reload sync completed for '{packageName}'!");
        }
    }
}
#endif

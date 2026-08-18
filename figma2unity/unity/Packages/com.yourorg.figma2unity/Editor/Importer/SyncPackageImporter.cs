using System;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Figma2Unity.Editor.Schema;
using Figma2Unity.Editor.Generator;

namespace Figma2Unity.Editor.Importer
{
    public static class SyncPackageImporter
    {
        public const string ExpectedMajorVersion = "1";

        [MenuItem("Figma2Unity/Import Sync Package...")]
        public static void ImportSyncPackageMenu()
        {
            string zipPath = EditorUtility.OpenFilePanel("Select Figma2Unity Sync Package (.f2u.zip)", "", "zip");
            if (string.IsNullOrEmpty(zipPath))
            {
                return;
            }

            try
            {
                ImportResult result = ImportSyncPackage(zipPath);
                if (result.Success)
                {
                    var report = result.ReportData;
                    int warnCount = report != null ? report.ValidationWarnings.Count : 0;
                    int missingFontCount = report != null ? report.MissingFonts.Count : 0;
                    int rasterCount = report != null ? report.RasterizedNodes.Count : 0;
                    int totalNodes = report != null ? report.TotalNodesProcessed : (result.Document?.rootNodes?.Count ?? 0);

                    string summaryText = $"Import Complete for '{result.Document.metadata?.figmaFileName ?? "Package"}'!\n\n" +
                                         $"Total Nodes Processed: {totalNodes}\n" +
                                         $"Rasterized Fallbacks: {rasterCount}\n" +
                                         $"Missing Fonts: {missingFontCount}\n" +
                                         $"Validation Warnings: {warnCount}";

                    bool openReport = EditorUtility.DisplayDialog("Figma2Unity Import Complete", summaryText, "Open Report", "Close");
                    if (openReport && result.ReportResult != null)
                    {
                        string openPath = !string.IsNullOrEmpty(result.ReportResult.HtmlPath) ? result.ReportResult.HtmlPath : result.ReportResult.MarkdownPath;
                        if (!string.IsNullOrEmpty(openPath) && File.Exists(openPath))
                        {
                            Application.OpenURL("file://" + Path.GetFullPath(openPath));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Figma2Unity] Package import failed: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Figma2Unity Import Failed", ex.Message, "OK");
            }
        }

        public class ImportResult
        {
            public bool Success;
            public string ErrorMessage;
            public IRDocument Document;
            public string DestinationAssetFolder;
            public Figma2Unity.Editor.Reporting.ImportReportData ReportData;
            public Figma2Unity.Editor.Reporting.ReportGenerator.ReportResult ReportResult;
        }

        public static ImportResult ImportSyncPackage(string zipFilePath, string customDestinationFolder = null)
        {
            if (!File.Exists(zipFilePath))
            {
                throw new FileNotFoundException($"Package zip file not found at path: {zipFilePath}");
            }

            string tempExtractDir = Path.Combine(Application.temporaryCachePath, "Figma2UnityTemp_" + Guid.NewGuid().ToString("N"));
            if (Directory.Exists(tempExtractDir))
            {
                Directory.Delete(tempExtractDir, true);
            }
            Directory.CreateDirectory(tempExtractDir);

            try
            {
                // 1. Unzip archive into temporary cache directory
                ZipFile.ExtractToDirectory(zipFilePath, tempExtractDir);

                string irJsonPath = Path.Combine(tempExtractDir, "ir-document.json");
                if (!File.Exists(irJsonPath))
                {
                    throw new InvalidDataException("Invalid .f2u.zip package: missing 'ir-document.json'");
                }

                // 2. Deserialize IR JSON Document
                string jsonContent = File.ReadAllText(irJsonPath);
                IRDocument document = ParseIRDocument(jsonContent);

                string packageName = Path.GetFileNameWithoutExtension(zipFilePath);
                if (packageName.EndsWith(".f2u", StringComparison.OrdinalIgnoreCase))
                {
                    packageName = packageName.Substring(0, packageName.Length - 4);
                }

                // 3. Initialize centralized import logger session
                Figma2Unity.Editor.Reporting.FigmaImportLogger.BeginSession(packageName, document?.schemaVersion);

                // 4. Validate Schema Version (FR2)
                if (!ValidateSchemaVersion(document.schemaVersion, out string versionError))
                {
                    Figma2Unity.Editor.Reporting.FigmaImportLogger.LogValidationWarning("SchemaVersion", versionError);
                    EditorUtility.DisplayDialog("Figma2Unity Schema Error", versionError, "OK");
                    return new ImportResult
                    {
                        Success = false,
                        ErrorMessage = versionError,
                        Document = document
                    };
                }

                string destFolder = customDestinationFolder;
                if (string.IsNullOrEmpty(destFolder))
                {
                    destFolder = Path.Combine("Assets", "Figma2UnityImports", packageName);
                }

                if (!Directory.Exists(destFolder))
                {
                    Directory.CreateDirectory(destFolder);
                }

                // 5. Copy Exported Assets (PNGs & SVGs)
                string tempExportsDir = Path.Combine(tempExtractDir, "exports");
                if (Directory.Exists(tempExportsDir))
                {
                    string destExportsDir = Path.Combine(destFolder, "exports");
                    CopyDirectoryRecursive(tempExportsDir, destExportsDir);
                }

                AssetDatabase.Refresh();

                // 6. Configure TextureImporter for imported PNG raster assets
                ConfigureRasterAssets(destFolder);

                // 7. Generate UI Toolkit UXML & USS structures
                string generatedFolder = Path.Combine("Assets", "Figma2Unity", "Generated", packageName);
                UIToolkitGenerator.Generate(document, generatedFolder, packageName);

                // 8. Generate ScriptableObject Token Assets (ColorPaletteSO, TypeRampSO, SpacingScaleSO, EffectStyleSO)
                TokenAssetGenerator.GenerateTokenAssets(document, generatedFolder);

                // 9. Match or generate TextMeshPro font assets for text nodes
                string fontsFolder = Path.Combine("Assets", "Figma2Unity", "Generated", "Fonts");
                ResolveTextNodeFonts(document, fontsFolder);

                // 10. Generate Markdown & HTML Import Reports
                var reportData = Figma2Unity.Editor.Reporting.FigmaImportLogger.EndSession();
                var reportResult = Figma2Unity.Editor.Reporting.ReportGenerator.GenerateReports(reportData, generatedFolder);

                return new ImportResult
                {
                    Success = true,
                    Document = document,
                    DestinationAssetFolder = destFolder,
                    ReportData = reportData,
                    ReportResult = reportResult
                };
            }
            finally
            {
                if (Directory.Exists(tempExtractDir))
                {
                    try
                    {
                        Directory.Delete(tempExtractDir, true);
                    }
                    catch
                    {
                        // Ignore temp cleanup errors
                    }
                }
            }
        }

        public static IRDocument ParseIRDocument(string jsonContent)
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new IRNodeConverter());
            return JsonConvert.DeserializeObject<IRDocument>(jsonContent, settings);
        }

        public static bool ValidateSchemaVersion(string version, out string errorMessage)
        {
            if (string.IsNullOrEmpty(version))
            {
                errorMessage = "IR Schema version is missing or empty.";
                return false;
            }

            string[] parts = version.Split('.');
            string major = parts[0];

            if (major != ExpectedMajorVersion)
            {
                errorMessage = $"Incompatible IR Schema version '{version}'. Importer expects major version {ExpectedMajorVersion}.x.x per FR2.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static void ConfigureRasterAssets(string rootFolder)
        {
            string exportsImagesDir = Path.Combine(rootFolder, "exports", "images");
            if (!Directory.Exists(exportsImagesDir)) return;

            string[] imageFiles = Directory.GetFiles(exportsImagesDir, "*.png", SearchOption.AllDirectories);
            foreach (string file in imageFiles)
            {
                string assetPath = file.Replace('\\', '/');
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePivot = new Vector2(0.5f, 0.5f);
                    importer.SaveAndReimport();
                }
            }
        }

        private static void ResolveTextNodeFonts(IRDocument document, string fontsFolder)
        {
            if (document?.rootNodes == null) return;

            Figma2Unity.Editor.Fonts.FontResolver.ClearReport();

            foreach (var rootNode in document.rootNodes)
            {
                ResolveNodeFontsRecursive(rootNode, fontsFolder);
            }
        }

        private static void ResolveNodeFontsRecursive(IRNode node, string fontsFolder)
        {
            if (node == null) return;

            if (node is TextNode textNode && !string.IsNullOrEmpty(textNode.fontFamily))
            {
                Figma2Unity.Editor.Fonts.FontResolver.ResolveFontForTextNode(textNode, fontsFolder);
            }

            if (node is FrameNode frameNode && frameNode.children != null)
            {
                foreach (var child in frameNode.children) ResolveNodeFontsRecursive(child, fontsFolder);
            }
            else if (node is GroupNode groupNode && groupNode.children != null)
            {
                foreach (var child in groupNode.children) ResolveNodeFontsRecursive(child, fontsFolder);
            }
            else if (node is ComponentInstanceNode compNode && compNode.children != null)
            {
                foreach (var child in compNode.children) ResolveNodeFontsRecursive(child, fontsFolder);
            }
        }

        private static void CopyDirectoryRecursive(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
                CopyDirectoryRecursive(subDir, destSubDir);
            }
        }
    }
}

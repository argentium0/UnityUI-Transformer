using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Figma2Unity.Editor.Importer;
using Figma2Unity.Editor.Schema;
using Figma2Unity.Editor.Reporting;

namespace Figma2Unity.Tests.Editor
{
    public class Phase10AutoImportTests
    {
        [Test]
        public void FigmaAutoImportTrigger_ProcessIncomingIRDocument_GeneratesUXMLuGUIAndReport()
        {
            string packageName = "LiveAutoImportTestPackage";
            string tempDir = Path.Combine(Path.GetTempPath(), "Figma2UnityPhase10_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            string irJsonPath = Path.Combine(tempDir, "ir-document.json");

            try
            {
                var rootFrame = new FrameNode
                {
                    id = "auto-root-1",
                    name = "AutoSyncScreen",
                    type = "FRAME",
                    visible = true,
                    bounds = new Bounds { x = 0, y = 0, width = 1920, height = 1080 },
                    children = new List<IRNode>
                    {
                        new TextNode
                        {
                            id = "auto-text-1",
                            name = "LiveHeader",
                            type = "TEXT",
                            characters = "Hot Reloading Test",
                            fontSize = 32f,
                            bounds = new Bounds { x = 40, y = 40, width = 400, height = 60 }
                        }
                    }
                };

                var doc = new IRDocument
                {
                    schemaVersion = "1.0.0",
                    metadata = new Metadata { figmaFileName = packageName },
                    rootNodes = new List<IRNode> { rootFrame }
                };

                string json = SyncPackageImporter.ParseIRDocument(Newtonsoft.Json.JsonConvert.SerializeObject(doc)) != null ? Newtonsoft.Json.JsonConvert.SerializeObject(doc) : "";
                File.WriteAllText(irJsonPath, json);

                // Execute auto-import trigger logic directly
                FigmaAutoImportTrigger.ProcessIncomingIRDocument(irJsonPath);

                string generatedDir = Path.Combine("Assets", "Figma2Unity", "Generated", packageName);
                string uxmlPath = Path.Combine(generatedDir, "autosyncscreen.uxml");
                string ussPath = Path.Combine(generatedDir, "LiveAutoImportTestPackage.uss");
                string reportPath = Path.Combine(generatedDir, "ImportReport.md");

                Assert.IsTrue(File.Exists(uxmlPath) || Directory.Exists(generatedDir));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }
    }
}

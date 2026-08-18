using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Figma2Unity.Editor.Generator;
using Figma2Unity.Editor.Schema;
using Figma2Unity.Editor.Reporting;

namespace Figma2Unity.Tests.Editor
{
    public class Phase9UGUITests
    {
        [Test]
        public void UGUIGenerator_Generate_TranslatesIRDocumentToUGUIHierarchyAndLogsReport()
        {
            string packageName = "UGUITestPackage";
            string tempDir = Path.Combine(Path.GetTempPath(), "Figma2UnityPhase9_" + Guid.NewGuid().ToString("N"));

            try
            {
                var rootFrame = new FrameNode
                {
                    id = "ugui-root",
                    name = "UGUIScreen",
                    type = "FRAME",
                    visible = true,
                    bounds = new Bounds { x = 0, y = 0, width = 1920, height = 1080 },
                    autoLayout = new AutoLayout
                    {
                        layoutMode = "VERTICAL",
                        gap = 10f,
                        padding = new Padding { top = 20, right = 20, bottom = 20, left = 20 }
                    },
                    children = new List<IRNode>
                    {
                        new TextNode
                        {
                            id = "ugui-text-1",
                            name = "TitleText",
                            type = "TEXT",
                            characters = "uGUI Prefab Test",
                            fontSize = 24f,
                            bounds = new Bounds { x = 20, y = 20, width = 300, height = 50 }
                        },
                        new UnsupportedNode
                        {
                            id = "ugui-unsup-1",
                            name = "Exotic3DNode",
                            type = "UNSUPPORTED",
                            bounds = new Bounds { x = 20, y = 80, width = 100, height = 100 }
                        }
                    }
                };

                var doc = new IRDocument
                {
                    schemaVersion = "1.0.0",
                    rootNodes = new List<IRNode> { rootFrame }
                };

                FigmaImportLogger.BeginSession(packageName, doc.schemaVersion);

                var result = UGUIGenerator.Generate(doc, tempDir, packageName);

                var report = FigmaImportLogger.EndSession();

                Assert.IsNotNull(result);
                Assert.IsTrue(result.Success);

                Assert.IsNotNull(report);
                Assert.IsTrue(report.TotalNodesProcessed >= 3); // Canvas root + 2 children
                Assert.IsTrue(report.RasterizedNodes.Count >= 1);
                Assert.AreEqual("Exotic3DNode", report.RasterizedNodes[0].NodeName);
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

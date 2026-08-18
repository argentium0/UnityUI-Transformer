using System;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using Figma2Unity.Editor.Schema;

namespace Figma2Unity.Editor.Generator
{
    public static class UIToolkitGenerator
    {
        public class GenerationResult
        {
            public bool Success;
            public string USSPath;
            public List<string> UXMLPaths = new List<string>();
        }

        public static GenerationResult Generate(IRDocument document, string destinationFolder, string packageName)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (string.IsNullOrEmpty(destinationFolder))
            {
                destinationFolder = Path.Combine("Assets", "Figma2Unity", "Generated", packageName ?? "SyncPackage");
            }

            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            var result = new GenerationResult();

            var utf8Encoding = new System.Text.UTF8Encoding(false);

            // 1. Generate single shared USS Stylesheet for the package
            string ussContent = USSStyleGenerator.GenerateUSS(document, packageName);
            string ussFileName = $"{packageName}.uss";
            string ussFullPath = Path.Combine(destinationFolder, ussFileName);
            File.WriteAllText(ussFullPath, ussContent, utf8Encoding);
            result.USSPath = ussFullPath.Replace('\\', '/');

            // 2. Generate one UXML file per top-level root IR node
            for (int i = 0; i < document.rootNodes.Count; i++)
            {
                var rootNode = document.rootNodes[i];
                string screenName = USSStyleGenerator.SanitizeClassName(rootNode.name ?? $"Screen_{i}", rootNode.id);
                string uxmlFileName = $"{screenName}.uxml";
                string uxmlFullPath = Path.Combine(destinationFolder, uxmlFileName);

                string relativeUssPath = result.USSPath;

                string uxmlContent = UXMLTreeGenerator.GenerateUXML(rootNode, relativeUssPath);
                File.WriteAllText(uxmlFullPath, uxmlContent, utf8Encoding);
                result.UXMLPaths.Add(uxmlFullPath.Replace('\\', '/'));
            }

            result.Success = true;

#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif

            return result;
        }
    }
}

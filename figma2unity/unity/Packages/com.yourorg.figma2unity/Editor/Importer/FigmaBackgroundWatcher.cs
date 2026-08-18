using System;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Figma2Unity.Editor.Importer
{
    [InitializeOnLoad]
    public static class FigmaBackgroundWatcher
    {
        private static FileSystemWatcher _watcher;

        static FigmaBackgroundWatcher()
        {
            InitializeWatcher();

            // Subscribe to domain reload and editor shutdown cleanup events
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorApplication.quitting += OnEditorQuitting;
        }

        private static void InitializeWatcher()
        {
            try
            {
                string importFolderPath = Path.Combine(Application.dataPath, "FigmaImport");
                if (!Directory.Exists(importFolderPath))
                {
                    importFolderPath = Path.Combine(Application.dataPath, "Figma2UnityImports");
                }

                if (!Directory.Exists(importFolderPath))
                {
                    Directory.CreateDirectory(importFolderPath);
                }

                _watcher = new FileSystemWatcher(importFolderPath)
                {
                    Filter = "ir-document.json",
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
                };

                _watcher.Created += OnFileChanged;
                _watcher.Changed += OnFileChanged;
                _watcher.EnableRaisingEvents = true;

                Debug.Log($"[Figma2Unity BackgroundWatcher] Active background watcher listening at '{importFolderPath}'");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Figma2Unity BackgroundWatcher] Unable to initialize background watcher: {ex.Message}");
            }
        }

        private static void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // Thread Safety: FileSystemWatcher events execute on a background worker thread.
            // Safely dispatch AssetDatabase.Refresh() to the Unity Editor main thread via EditorApplication.delayCall.
            EditorApplication.delayCall += () =>
            {
                Debug.Log($"[Figma2Unity BackgroundWatcher] Detected background IR payload update at '{e.FullPath}'. Triggering AssetDatabase.Refresh()...");
                AssetDatabase.Refresh();
            };
        }

        private static void OnBeforeAssemblyReload()
        {
            DisposeWatcher();
        }

        private static void OnEditorQuitting()
        {
            DisposeWatcher();
        }

        private static void DisposeWatcher()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Created -= OnFileChanged;
                _watcher.Changed -= OnFileChanged;
                _watcher.Dispose();
                _watcher = null;
                Debug.Log("[Figma2Unity BackgroundWatcher] Background watcher cleanly disposed.");
            }
        }
    }
}
#endif

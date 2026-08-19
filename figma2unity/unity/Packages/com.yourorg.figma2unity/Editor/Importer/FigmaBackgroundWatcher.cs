using System;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Figma2Unity.Editor.Generator;

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
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            EditorApplication.quitting += OnEditorQuitting;
        }

        private static void InitializeWatcher()
        {
            try
            {
                // Staging directory inside project root Temp folder (outside Assets/)
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string stagingFolderPath = Path.Combine(projectRoot, "Temp", "Figma2UnitySync");

                if (!Directory.Exists(stagingFolderPath))
                {
                    Directory.CreateDirectory(stagingFolderPath);
                }

                _watcher = new FileSystemWatcher(stagingFolderPath)
                {
                    Filter = "sync.complete",
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
                };

                _watcher.Created += OnSyncCompleteDetected;
                _watcher.Changed += OnSyncCompleteDetected;
                _watcher.EnableRaisingEvents = true;

                Debug.Log($"[Figma2Unity BackgroundWatcher] Active staging watcher listening for 'sync.complete' at '{stagingFolderPath}'");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Figma2Unity BackgroundWatcher] Unable to initialize staging watcher: {ex.Message}");
            }
        }

        private static DateTime _lastTriggerTime = DateTime.MinValue;

        private static void OnSyncCompleteDetected(object sender, FileSystemEventArgs e)
        {
            DateTime currentTime = DateTime.UtcNow;
            if ((currentTime - _lastTriggerTime).TotalSeconds < 1.0)
            {
                return;
            }
            _lastTriggerTime = currentTime;

            string lockFilePath = e.FullPath;

            // Thread Safety: FileSystemWatcher runs on a background thread.
            // Safely dispatch the bulletproof import sequence to the main thread via EditorApplication.delayCall.
            EditorApplication.delayCall += () =>
            {
                try
                {
                    Debug.Log($"[Figma2Unity BackgroundWatcher] Detected 'sync.complete' at '{lockFilePath}'. Initiating bulletproof import sequence...");
                    string stagingFolder = Path.GetDirectoryName(lockFilePath);
                    UIToolkitGenerator.RegenerateStylesheet(stagingFolder);
                    
                    try { File.Delete(lockFilePath); } catch { }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Figma2Unity BackgroundWatcher] Error executing staging import: {ex.Message}\n{ex.StackTrace}");
                }
            };
        }

        private static void OnBeforeAssemblyReload()
        {
            DisposeWatcher();
        }

        private static void OnAfterAssemblyReload()
        {
            if (_watcher == null)
            {
                InitializeWatcher();
            }
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
                _watcher.Created -= OnSyncCompleteDetected;
                _watcher.Changed -= OnSyncCompleteDetected;
                _watcher.Dispose();
                _watcher = null;
                Debug.Log("[Figma2Unity BackgroundWatcher] Staging watcher cleanly disposed.");
            }
        }
    }
}
#endif

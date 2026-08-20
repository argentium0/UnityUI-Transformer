using System;

namespace UnityEditor
{
    [Flags]
    public enum ImportAssetOptions
    {
        Default = 0,
        ForceUpdate = 1,
        ForceSynchronousImport = 8
    }

    public enum TextureImporterType
    {
        Default = 0,
        Sprite = 8
    }

    public enum SpriteImportMode
    {
        None = 0,
        Single = 1,
        Multiple = 2
    }

    public class AssetImporter
    {
        public static AssetImporter? GetAtPath(string path)
        {
            return new TextureImporter();
        }

        public virtual void SaveAndReimport() { }
    }

    public class TextureImporter : AssetImporter
    {
        public TextureImporterType textureType { get; set; } = TextureImporterType.Sprite;
        public SpriteImportMode spriteImportMode { get; set; } = SpriteImportMode.Single;
        public bool alphaIsTransparency { get; set; } = true;

        public override void SaveAndReimport() { }
    }

    public static class AssetDatabase
    {
        public static void Refresh() { }
        public static void Refresh(ImportAssetOptions options) { }
        public static void ImportAsset(string path) { }
        public static void ImportAsset(string path, ImportAssetOptions options) { }
    }
}

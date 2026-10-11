namespace Terraria.ModLoader
{
    internal enum ModSide { Client }
    internal sealed class AutoloadAttribute : Attribute
    {
        public ModSide Side { get; set; }
    }
    internal class ModSystem
    {
        public Mod Mod { get; set; }
        public virtual void Load() { }
        public virtual void Unload() { }
    }
    internal sealed class Mod
    {
        public Version Version { get; } = new(2, 1, 3);
        public Core.TmodFile File { get; set; }
        public TerraVoice.TestLogger Logger { get; } = new();
    }
}

namespace Terraria.ModLoader.Core
{
    internal sealed class TmodFile
    {
        public Dictionary<string, FileEntry> files { get; } = new();
        public Stream GetStream(FileEntry entry) => System.IO.File.OpenRead(entry.SourcePath);
        internal sealed class FileEntry
        {
            public string Name { get; init; }
            public string SourcePath { get; init; }
            public int Length => checked((int)new FileInfo(SourcePath).Length);
        }
    }
}

namespace Terraria.ModLoader.UI
{
    internal static class Interface
    {
        public static LoadMods loadMods { get; } = new();
    }
    internal sealed class LoadMods
    {
        public string SubProgressText { get; set; }
    }
}

namespace Terraria.Localization
{
    internal static class Language
    {
        public static System.Globalization.CultureInfo ActiveCulture { get; } = new("en-US");
    }
}

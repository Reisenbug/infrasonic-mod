using System.Collections.Generic;
using System.IO;
using Terraria.ModLoader;
using System.Linq;
using System.Runtime.InteropServices;
using System;
using System.Reflection;
using POpusCodec;
using Terraria.Localization;

namespace TerraVoice.Native;

[Autoload(Side = ModSide.Client)]
internal class NativeFeatureSystem : ModSystem
{
    private static readonly Dictionary<string, Assembly> assemblyHints = new()
    {
        ["libopus"] = typeof(OpusEncoder).Assembly,
        ["librnnoise"] = typeof(rnnoise).Assembly
    };

    private readonly Dictionary<string, string> _loadLibrariesTextLocalization = new() {
        {"default", "Loading native libraries..."},
        {"zh-Hans", "正在加载本地程序集..."}
    };

    private readonly Dictionary<string, IntPtr> loadedLibs = new();

    public override void Load()
    {
        Mod.Logger.Info(Language.ActiveCulture.Name is "zh-Hans"
                ? _loadLibrariesTextLocalization["zh-Hans"]
                : _loadLibrariesTextLocalization["default"]);

        Dictionary<string, string> binaryMap = ExtractPlatformBinaries();

        foreach (string key in assemblyHints.Keys)
        {
            Assembly assembly = assemblyHints[key];

            string path = binaryMap[key];

            NativeLibrary.SetDllImportResolver(assembly, (name, assembly, dllSearchPath) =>
            {
                if (name != key)
                    return IntPtr.Zero;

                if (!loadedLibs.ContainsKey(key))
                {
                    IntPtr handle = NativeLibrary.Load(path);

                    Mod.Logger.Info($"Successfully loaded native library: {path}. Handle: {handle}");

                    loadedLibs[key] = handle;
                }

                return loadedLibs[key];
            });
        }

        ALMonoMicrophone.LoadOpenAL(binaryMap["libopenal"]);
    }

    public override void Unload()
    {
        foreach (IntPtr handle in loadedLibs.Values)
        {
            NativeLibrary.Free(handle);
        }

        ALMonoMicrophone.UnloadOpenAL();
    }

    private void CopyLibFromTmod(string name, string path)
    {
        using Stream stream = Mod.GetFileStream(name);
        using FileStream destination = File.Create(path);
        stream.CopyTo(destination);

        Mod.Logger.Info($"Successfully installed native library: {path}.");
    }

    private Dictionary<string, string> ExtractPlatformBinaries()
    {
        Dictionary<string, string> binaries = new();

        string platform = GetPlatformDirectory();
        string path = $"lib/Native/{platform}/";
        string cachePath = Path.Combine(TerraVoice.CachePath, "Native", platform, Mod.Version.ToString());
        Directory.CreateDirectory(cachePath);

        IEnumerable<string> nativeLibraries = Mod.GetFileNames().Where(name => name.StartsWith(path));

        foreach (string name in nativeLibraries)
        {
            string destinationPath = Path.Combine(cachePath, Path.GetFileName(name));

            binaries.Add(Path.GetFileNameWithoutExtension(name), destinationPath);

            if (!File.Exists(destinationPath))
            {
                CopyLibFromTmod(name, destinationPath);
            }
        }

        return binaries;
    }

    private static string GetPlatformDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "win64";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "linux64";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            return "osx-arm64";
        throw new PlatformNotSupportedException($"InfraSonic has no native libraries for {RuntimeInformation.OSDescription} / {RuntimeInformation.ProcessArchitecture}.");
    }
}

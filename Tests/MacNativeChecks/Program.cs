using System.Runtime.InteropServices;
using POpusCodec;
using POpusCodec.Enums;
using TerraVoice.Native;

string repo = Path.GetFullPath(args[0]);
Console.WriteLine($"Runtime={Environment.Version} OS={RuntimeInformation.OSDescription} Arch={RuntimeInformation.ProcessArchitecture}");
var originalMap = new Dictionary<string, string>();
try
{
    foreach (string file in Directory.GetFiles(Path.Combine(repo, "src/TerraVoice/lib/Native"), "*", SearchOption.AllDirectories))
        originalMap.Add(Path.GetFileNameWithoutExtension(file), file);
    throw new Exception("Expected the original macOS extraction to fail on duplicate library names.");
}
catch (ArgumentException exception)
{
    Console.WriteLine($"Original macOS extraction reproduced: {exception.Message}");
}

string modRoot = Path.Combine(repo, "src/TerraVoice");
TerraVoice.TerraVoice.CachePath = Path.Combine(Path.GetTempPath(), $"infrasonic-native-check-{Guid.NewGuid():N}");
var modFile = new Terraria.ModLoader.Core.TmodFile();
foreach (string file in Directory.GetFiles(Path.Combine(modRoot, "lib/Native"), "*", SearchOption.AllDirectories))
{
    string relative = Path.GetRelativePath(modRoot, file).Replace('\\', '/');
    modFile.files.Add(relative, new() { Name = relative, SourcePath = file });
}
var system = new NativeFeatureSystem { Mod = new() { File = modFile } };
system.Load();
string cache = Path.Combine(TerraVoice.TerraVoice.CachePath, "Native", "osx-arm64", "2.1.3");
string[] extracted = Directory.GetFiles(cache);
if (extracted.Length != 3 || extracted.Any(file => Path.GetExtension(file) != ".dylib"))
    throw new Exception("Production loader mixed platforms or failed to extract all macOS libraries.");
Console.WriteLine("Production loader extracted only the three ARM64 dylibs into a versioned platform cache.");

short[] samples = Enumerable.Range(0, 960).Select(i => (short)(Math.Sin(i * 2 * Math.PI * 440 / 48000) * 8000)).ToArray();
var encoder = new OpusEncoder(SamplingRate.Sampling48000, Channels.Mono, OpusApplicationType.Voip, Delay.Delay20ms);
var decoder = new OpusDecoder(SamplingRate.Sampling48000, Channels.Mono);
encoder.Bitrate = 24000;
byte[] packet = encoder.Encode(samples);
short[] decoded = decoder.DecodePacket(packet);
if (packet.Length == 0 || decoded.Length != samples.Length || !decoded.Any(sample => sample != 0))
    throw new Exception("Opus round-trip failed.");
Console.WriteLine($"Opus round-trip passed: {samples.Length} samples -> {packet.Length} bytes -> {decoded.Length} samples.");

rnnoise.rnnoise_create();
try
{
    rnnoise.rnnoise_process_frame(samples);
    Console.WriteLine("Original RNNoise wrapper processed two 480-sample frames.");
}
finally
{
    rnnoise.rnnoise_destroy();
}

try
{
    Console.WriteLine($"Original Silk.NET OpenAL wrapper loaded. Capture devices: {string.Join(", ", ALMonoMicrophone.GetDevices())}");
}
finally
{
    encoder.Dispose();
    decoder.Dispose();
    system.Unload();
}
Console.WriteLine("MAC_NATIVE_CHECKS_PASSED. No microphone opened or recording started.");

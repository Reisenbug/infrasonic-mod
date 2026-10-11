using System.Runtime.InteropServices;
using POpusCodec;
using POpusCodec.Enums;
using TerraVoice.Native;

string repo = Path.GetFullPath(args[0]);
string native = Path.GetFullPath(args[1]);
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

NativeLibrary.SetDllImportResolver(typeof(OpusEncoder).Assembly, (name, _, _) =>
    name == "libopus" ? NativeLibrary.Load(Path.Combine(native, "libopus.dylib")) : IntPtr.Zero);
NativeLibrary.SetDllImportResolver(typeof(rnnoise).Assembly, (name, _, _) =>
    name == "librnnoise" ? NativeLibrary.Load(Path.Combine(native, "librnnoise.dylib")) : IntPtr.Zero);

short[] samples = Enumerable.Range(0, 960).Select(i => (short)(Math.Sin(i * 2 * Math.PI * 440 / 48000) * 8000)).ToArray();
using var encoder = new OpusEncoder(SamplingRate.Sampling48000, Channels.Mono, OpusApplicationType.Voip, Delay.Delay20ms);
using var decoder = new OpusDecoder(SamplingRate.Sampling48000, Channels.Mono);
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

ALMonoMicrophone.LoadOpenAL(Path.Combine(native, "libopenal.dylib"));
try
{
    Console.WriteLine($"Original Silk.NET OpenAL wrapper loaded. Capture devices: {string.Join(", ", ALMonoMicrophone.GetDevices())}");
}
finally
{
    ALMonoMicrophone.UnloadOpenAL();
}
Console.WriteLine("MAC_NATIVE_CHECKS_PASSED. No microphone opened or recording started.");

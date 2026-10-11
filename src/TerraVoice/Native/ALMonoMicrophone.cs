using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerraVoice.Src.Native;

namespace TerraVoice.Native;

internal class ALMonoMicrophone : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr OpenDevice(IntPtr name, uint frequency, int format, int bufferSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void DeviceAction(IntPtr device);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate byte CloseDevice(IntPtr device);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void GetInteger(IntPtr device, int parameter, int size, out int value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void CaptureSamples(IntPtr device, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] short[] samples, int count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GetString(IntPtr device, int parameter);

    private static IntPtr library;
    private static OpenDevice openDevice;
    private static DeviceAction captureStart;
    private static DeviceAction captureStop;
    private static CloseDevice closeDevice;
    private static GetInteger getInteger;
    private static CaptureSamples captureSamples;
    private static GetString getString;

    public event BufferReady OnBufferReady;
    private readonly int desiredFrameSize;
    private readonly IntPtr device;
    private readonly short[] buffer;
    private CancellationTokenSource source;

    public ALMonoMicrophone(string microphone, int captureFrameDurationMs, uint sampleRate)
    {
        desiredFrameSize = (int)(captureFrameDurationMs / 1000f * sampleRate);
        using var microphoneNative = NativeString.AllocFromString(microphone);
        device = openDevice(microphoneNative.Handle, sampleRate, 0x1101, desiredFrameSize);
        if (device == IntPtr.Zero)
            TerraVoice.Instance.Logger.Warn($"Failed to open device '{microphone}'!");
        buffer = new short[desiredFrameSize];
    }

    public void StartRecording()
    {
        captureStart(device);
        source = new();
        CancellationToken token = source.Token;
        Task.Factory.StartNew(() =>
        {
            var spinWait = new SpinWait();
            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    captureStop(device);
                    Array.Clear(buffer);
                    return;
                }
                getInteger(device, 0x312, 1, out int samplesAvailable);
                if (samplesAvailable >= desiredFrameSize)
                {
                    captureSamples(device, buffer, desiredFrameSize);
                    short[] copy = new short[desiredFrameSize];
                    Buffer.BlockCopy(buffer, 0, copy, 0, copy.Length * sizeof(short));
                    OnBufferReady?.Invoke(copy);
                }
                spinWait.SpinOnce();
            }
        }, source.Token);
    }

    public void StopRecording() => source?.Cancel();

    public void Dispose()
    {
        source?.Cancel();
        closeDevice(device);
    }

    public static List<string> GetDevices()
    {
        var devices = new List<string>();
        IntPtr current = getString(IntPtr.Zero, 0x310);
        while (current != IntPtr.Zero && Marshal.ReadByte(current) != 0)
        {
            string name = Marshal.PtrToStringUTF8(current);
            devices.Add(name);
            current = IntPtr.Add(current, Encoding.UTF8.GetByteCount(name) + 1);
        }
        return devices;
    }

    private static T Bind<T>(string name) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(library, name));

    public static void LoadOpenAL(string openalPath)
    {
        library = NativeLibrary.Load(openalPath);
        openDevice = Bind<OpenDevice>("alcCaptureOpenDevice");
        captureStart = Bind<DeviceAction>("alcCaptureStart");
        captureStop = Bind<DeviceAction>("alcCaptureStop");
        closeDevice = Bind<CloseDevice>("alcCaptureCloseDevice");
        getInteger = Bind<GetInteger>("alcGetIntegerv");
        captureSamples = Bind<CaptureSamples>("alcCaptureSamples");
        getString = Bind<GetString>("alcGetString");
    }

    public static void UnloadOpenAL()
    {
        if (library != IntPtr.Zero)
            NativeLibrary.Free(library);
        library = IntPtr.Zero;
    }

    public delegate void BufferReady(short[] buffer);
}

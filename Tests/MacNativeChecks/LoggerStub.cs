namespace TerraVoice;

internal sealed class TerraVoice
{
    public static TerraVoice Instance { get; } = new();
    public TestLogger Logger { get; } = new();
    public static string CachePath { get; set; }
}

internal sealed class TestLogger
{
    public void Warn(string message) => Console.WriteLine(message);
    public void Info(string message) => Console.WriteLine(message);
}

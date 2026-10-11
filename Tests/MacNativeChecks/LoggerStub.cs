namespace TerraVoice;

internal sealed class TerraVoice
{
    public static TerraVoice Instance { get; } = new();
    public TestLogger Logger { get; } = new();
}

internal sealed class TestLogger
{
    public void Warn(string message) => Console.WriteLine(message);
}

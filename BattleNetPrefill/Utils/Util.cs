using System.Runtime.InteropServices;

public static class OperatingSystem
{
    public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
}
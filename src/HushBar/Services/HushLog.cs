using System.Diagnostics;

namespace HushBar.Services;

/// <summary>
/// Thin diagnostic logging shim (analog to the macOS hushLog). Writes to the
/// debugger/trace output. Swap for a file or ETW sink if richer logging is needed.
/// </summary>
public static class HushLog
{
    public static void Write(string message) => Trace.WriteLine($"hushBar: {message}");
}

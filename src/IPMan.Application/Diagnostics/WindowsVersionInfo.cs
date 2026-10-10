namespace IPMan.Application.Diagnostics;

/// <summary>Windows version facts, including the operating-system architecture.</summary>
public sealed record WindowsVersionInfo(
    string ProductName,
    string DisplayVersion,
    string BuildNumber,
    string Architecture);

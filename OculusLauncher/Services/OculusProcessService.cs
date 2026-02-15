using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace OculusLauncher.Services;

public class OculusProcessService : IOculusProcessService
{
    private static readonly string[] ExeNames = ["OculusClient.exe", "client.exe"];
    private static readonly string[] ProcessNames = ["OculusClient", "client"];

    private static readonly string[] FallbackBasePaths =
    [
        @"C:\Program Files\Meta Horizon",
        @"C:\Program Files\Oculus"
    ];

    private const string OculusClientSubPath = @"Support\oculus-client";

    public string? FindOculusClientPath(string? manualOverride = null)
    {
        // Manual override takes priority
        if (!string.IsNullOrWhiteSpace(manualOverride) && File.Exists(manualOverride))
            return manualOverride;

        // Try registry first (both exe names)
        var registryBase = GetBasePathFromRegistry();
        if (registryBase != null)
        {
            var found = FindExeInBase(registryBase);
            if (found != null) return found;
        }

        // Fallback to known base paths
        foreach (var basePath in FallbackBasePaths)
        {
            var found = FindExeInBase(basePath);
            if (found != null) return found;
        }

        return null;
    }

    public bool IsOculusRunning()
    {
        return ProcessNames.Any(name => Process.GetProcessesByName(name).Length > 0);
    }

    public void KillOculusClient()
    {
        foreach (var name in ProcessNames)
        {
            var processes = Process.GetProcessesByName(name);
            foreach (var proc in processes)
            {
                try
                {
                    // For the generic "client" process name, verify it's actually
                    // the Oculus client by checking its file path
                    if (name == "client")
                    {
                        try
                        {
                            var procPath = proc.MainModule?.FileName;
                            if (procPath == null || !procPath.Contains("oculus", StringComparison.OrdinalIgnoreCase))
                            {
                                continue; // Skip non-Oculus "client" processes
                            }
                        }
                        catch
                        {
                            continue; // Can't verify, skip to be safe
                        }
                    }

                    proc.Kill();
                    proc.WaitForExit(5000);
                }
                finally
                {
                    proc.Dispose();
                }
            }
        }
    }

    public void StartOculusClient(string exePath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = true
        });
    }

    private static string? FindExeInBase(string basePath)
    {
        var supportDir = Path.Combine(basePath, OculusClientSubPath);
        foreach (var exeName in ExeNames)
        {
            var fullPath = Path.Combine(supportDir, exeName);
            if (File.Exists(fullPath))
                return fullPath;
        }
        return null;
    }

    private static string? GetBasePathFromRegistry()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\WOW6432Node\Oculus VR, LLC\Oculus");
            return key?.GetValue("Base") as string;
        }
        catch
        {
            return null;
        }
    }
}

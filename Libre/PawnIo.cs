using System;
using Microsoft.Win32;
using MoBro.Plugin.SDK.Enums;

namespace MoBro.Plugin.LibreHardwareMonitor.Libre;

public static class PawnIo
{
  private const string RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO";
  private static readonly Version MinVersion = new(2, 0, 0);

  public static DependencyStatus GetStatus() => GetInstalledVersion() switch
  {
    null => DependencyStatus.Missing,
    var v when v < MinVersion => DependencyStatus.Outdated,
    _ => DependencyStatus.Ok
  };

  private static Version? GetInstalledVersion()
  {
    using var subKey = Registry.LocalMachine.OpenSubKey(RegistryPath);
    if (Version.TryParse(subKey?.GetValue("DisplayVersion") as string, out var version))
    {
      return version;
    }

    using var registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
    using var subKeyWow64 = registryKey.OpenSubKey(RegistryPath);

    if (Version.TryParse(subKeyWow64?.GetValue("DisplayVersion") as string, out version))
    {
      return version;
    }

    return null;
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LibreHardwareMonitor.Hardware;
using MoBro.Plugin.LibreHardwareMonitor.Extensions;
using MoBro.Plugin.LibreHardwareMonitor.Model;
using MoBro.Plugin.SDK.Models;
using MoBro.Plugin.SDK.Models.Metrics;
using MoBro.Plugin.SDK.Services;

namespace MoBro.Plugin.LibreHardwareMonitor;

public class LibreHardwareMonitor : IDisposable
{
  private static readonly Regex IdSanitationRegex = new(@"[^\w\.\-]", RegexOptions.Compiled);

  private readonly Computer _computer;

  public LibreHardwareMonitor()
  {
    _computer = new Computer();
  }

  public void Update(IMoBroSettings settings)
  {
    _computer.IsCpuEnabled = settings.GetValue<bool>("cpu_enabled");
    _computer.IsGpuEnabled = settings.GetValue<bool>("gpu_enabled");
    _computer.IsMemoryEnabled = settings.GetValue<bool>("ram_enabled");
    _computer.IsMotherboardEnabled = settings.GetValue<bool>("motherboard_enabled");
    _computer.IsStorageEnabled = settings.GetValue<bool>("hdd_enabled");
    _computer.IsNetworkEnabled = settings.GetValue<bool>("network_enabled");
    _computer.IsControllerEnabled = settings.GetValue<bool>("controller_enabled");
    _computer.IsPsuEnabled = settings.GetValue<bool>("psu_enabled");
    _computer.IsBatteryEnabled = settings.GetValue<bool>("battery_enabled");

    _computer.Open();
  }

  public IEnumerable<IMoBroItem> GetMetricItems()
  {
    var sensors = GetSensors();
    var metrics = sensors
      .Select(sensor => sensor.AsMetric())
      .Select(m => m as IMoBroItem);

    var groups = sensors
      .Select(sensor => sensor.AsGroup())
      .DistinctBy(g => g.Id)
      .Select(m => m as IMoBroItem);

    return metrics.Concat(groups);
  }

  public IEnumerable<MetricValue> GetMetricValues()
  {
    return GetSensors().Select(s => s.AsMetricValue());
  }

  private IEnumerable<Sensor> GetSensors()
  {
    return _computer.Hardware.SelectMany(h => GetSensors(h.HardwareType, h));
  }

  private static IEnumerable<Sensor> GetSensors(HardwareType rootType, IHardware hardware)
  {
    // first update the sensors information 
    hardware.Update();

    // parse sensors 
    var list = hardware.Sensors.Select(sensor => new Sensor(
      SanitizeId(sensor.Identifier.ToString()),
      sensor.Name,
      sensor.Value,
      sensor.SensorType,
      rootType,
      SanitizeId(sensor.Hardware.Identifier.ToString()),
      sensor.Hardware.Name
    )).ToList();

    // recursively parse and add sensors of sub-hardware
    foreach (var sub in hardware.SubHardware)
    {
      list.AddRange(GetSensors(rootType, sub));
    }

    return list;
  }

  private static string SanitizeId(string id)
  {
    return IdSanitationRegex.Replace(id, "");
  }

  public void Dispose()
  {
    _computer.Close();
  }
}
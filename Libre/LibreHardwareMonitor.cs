using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LibreHardwareMonitor.Hardware;
using Microsoft.Extensions.Logging;
using MoBro.Plugin.SDK.Models;
using MoBro.Plugin.SDK.Models.Metrics;
using MoBro.Plugin.SDK.Services;

namespace MoBro.Plugin.LibreHardwareMonitor.Libre;

public class LibreHardwareMonitor(ILogger logger) : IDisposable
{
  private static readonly Regex IdSanitationRegex = new(@"[^\w\.\-]", RegexOptions.Compiled);

  private static readonly TimeSpan ReadErrorLogCooldown = TimeSpan.FromMinutes(5);
  private readonly Dictionary<string, DateTimeOffset> _lastReadError = new();

  private readonly Computer _computer = new();

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

    _computer.Reset();
    _computer.Open();
  }

  public IEnumerable<IMoBroItem> GetMetricItems()
  {
    var sensors = GetSensors();
    var sensorsArray = sensors as Sensor[] ?? sensors.ToArray();
    var metrics = sensorsArray
      .Select(IMoBroItem (sensor) => sensor.AsMetric());

    var groups = sensorsArray
      .Select(IMoBroItem (sensor) => sensor.AsGroup())
      .DistinctBy(g => g.Id);

    return groups.Concat(metrics);
  }

  public IEnumerable<MetricValue> GetMetricValues()
  {
    return GetSensors().Select(s =>
    {
      try
      {
        return s.AsMetricValue();
      }
      catch (Exception e)
      {
        // return an empty value if the sensor failed to read instead of crashing the plugin 
        ReadFailed("Sensor", s.Id, e);
        return new MetricValue(s.Id, null);
      }
    });
  }

  private IEnumerable<Sensor> GetSensors()
  {
    return _computer.Hardware.SelectMany(h =>
    {
      try
      {
        return GetSensors(h.HardwareType, h);
      }
      catch (Exception e)
      {
        // skip hardware that failed to read instead of crashing the plugin
        ReadFailed(h.HardwareType.ToString(), h.Identifier.ToString(), e);
        return [];
      }
    });
  }

  private void ReadFailed(string type, string id, Exception ex)
  {
    var now = DateTimeOffset.UtcNow;
    lock (_lastReadError)
    {
      var key = type + id + ex.GetType().Name;
      if (_lastReadError.TryGetValue(key, out var last) && now - last < ReadErrorLogCooldown)
      {
        return;
      }

      _lastReadError[key] = now;
    }

    logger.LogWarning(ex, "Failed to read {Type}: {Id}", type, id);
  }

  private static List<Sensor> GetSensors(HardwareType rootType, IHardware hardware)
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
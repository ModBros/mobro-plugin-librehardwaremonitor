using System;
using LibreHardwareMonitor.Hardware;
using MoBro.Plugin.SDK.Builders;
using MoBro.Plugin.SDK.Enums;
using MoBro.Plugin.SDK.Models.Categories;
using MoBro.Plugin.SDK.Models.Metrics;

namespace MoBro.Plugin.LibreHardwareMonitor.Libre;

internal static class SensorExtensions
{
  public static Metric AsMetric(this Sensor sensor)
  {
    return MoBroItem
      .CreateMetric()
      .WithId(sensor.Id)
      .WithLabel(sensor.Name)
      .OfType(GetMetricType(sensor.SensorType))
      .OfCategory(GetCategory(sensor.HardwareType))
      .OfGroup(sensor.GroupId)
      .Build();
  }

  public static Group AsGroup(this Sensor sensor)
  {
    return MoBroItem.CreateGroup()
      .WithId(sensor.GroupId)
      .WithLabel(sensor.GroupName)
      .Build();
  }

  public static MetricValue AsMetricValue(this Sensor sensor)
  {
    return new MetricValue(sensor.Id, GetMetricValue(sensor));
  }

  private static object? GetMetricValue(in Sensor sensor)
  {
    if (sensor.Value == null) return null;

    var doubleVal = Convert.ToDouble(sensor.Value);
    return sensor.SensorType switch
    {
      SensorType.Throughput => doubleVal * 8, // bytes => bit
      SensorType.Clock => doubleVal * 1_000_000, // MHz => Hertz
      SensorType.SmallData => doubleVal * 1_000_000, // MB => Byte
      SensorType.Data => doubleVal * 1_000_000_000, // GB => Byte
      SensorType.TimeSpan => TimeSpan.FromSeconds(doubleVal), // convert to TimeSpan
      _ => doubleVal
    };
  }

  private static CoreMetricType GetMetricType(SensorType sensorType)
  {
    switch (sensorType)
    {
      case SensorType.Voltage:
        return CoreMetricType.ElectricPotential;
      case SensorType.Clock:
      case SensorType.Frequency:
        return CoreMetricType.Frequency;
      case SensorType.Temperature:
        return CoreMetricType.Temperature;
      case SensorType.Load:
      case SensorType.Control:
      case SensorType.Level:
        return CoreMetricType.Usage;
      case SensorType.Power:
        return CoreMetricType.Power;
      case SensorType.SmallData:
      case SensorType.Data:
        return CoreMetricType.Data;
      case SensorType.Throughput:
        return CoreMetricType.DataFlow;
      case SensorType.Fan:
        return CoreMetricType.Rotation;
      case SensorType.Factor:
        return CoreMetricType.Multiplier;
      case SensorType.Current:
        return CoreMetricType.ElectricCurrent;
      case SensorType.Flow:
        return CoreMetricType.VolumeFlow;
      case SensorType.TimeSpan:
        return CoreMetricType.Duration;
      case SensorType.Energy:
      case SensorType.Noise:
      case SensorType.Timing:
      case SensorType.Conductivity:
      case SensorType.Humidity:
      default:
        return CoreMetricType.Numeric;
    }
  }

  private static CoreCategory GetCategory(HardwareType hardwareType)
  {
    switch (hardwareType)
    {
      case HardwareType.Cpu:
        return CoreCategory.Cpu;
      case HardwareType.GpuNvidia:
      case HardwareType.GpuAmd:
      case HardwareType.GpuIntel:
        return CoreCategory.Gpu;
      case HardwareType.Memory:
        return CoreCategory.Ram;
      case HardwareType.Storage:
        return CoreCategory.Storage;
      case HardwareType.Motherboard:
        return CoreCategory.Mainboard;
      case HardwareType.Network:
        return CoreCategory.Network;
      case HardwareType.Battery:
        return CoreCategory.Battery;
      case HardwareType.SuperIO:
      case HardwareType.Cooler:
      case HardwareType.EmbeddedController:
      case HardwareType.Psu:
      default:
        return CoreCategory.Miscellaneous;
    }
  }
}
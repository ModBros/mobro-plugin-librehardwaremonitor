using System;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using MoBro.Plugin.SDK.Builders;
using MoBro.Plugin.SDK.Enums;
using MoBro.Plugin.SDK.Models.Categories;
using MoBro.Plugin.SDK.Models.Metrics;

namespace MoBro.Plugin.LibreHardwareMonitor.Libre;

internal static class SensorExtensions
{
  private static readonly Dictionary<SensorType, CoreMetricType> MetricTypeMap = new()
  {
    { SensorType.Voltage, CoreMetricType.ElectricPotential },
    { SensorType.Clock, CoreMetricType.Frequency },
    { SensorType.Frequency, CoreMetricType.Frequency },
    { SensorType.Temperature, CoreMetricType.Temperature },
    { SensorType.Load, CoreMetricType.Usage },
    { SensorType.Control, CoreMetricType.Usage },
    { SensorType.Level, CoreMetricType.Usage },
    { SensorType.Power, CoreMetricType.Power },
    { SensorType.SmallData, CoreMetricType.Data },
    { SensorType.Data, CoreMetricType.Data },
    { SensorType.Throughput, CoreMetricType.DataFlow },
    { SensorType.Fan, CoreMetricType.Rotation },
    { SensorType.Factor, CoreMetricType.Multiplier },
    { SensorType.Current, CoreMetricType.ElectricCurrent },
    { SensorType.Flow, CoreMetricType.VolumeFlow },
    { SensorType.TimeSpan, CoreMetricType.Duration }
  };

  private static readonly Dictionary<HardwareType, CoreCategory> HardwareCategoryMap = new()
  {
    { HardwareType.Cpu, CoreCategory.Cpu },
    { HardwareType.GpuNvidia, CoreCategory.Gpu },
    { HardwareType.GpuAmd, CoreCategory.Gpu },
    { HardwareType.GpuIntel, CoreCategory.Gpu },
    { HardwareType.Memory, CoreCategory.Ram },
    { HardwareType.Storage, CoreCategory.Storage },
    { HardwareType.Motherboard, CoreCategory.Mainboard },
    { HardwareType.Network, CoreCategory.Network },
    { HardwareType.Battery, CoreCategory.Battery }
  };

  extension(Sensor sensor)
  {
    public Metric AsMetric()
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

    public Group AsGroup()
    {
      return MoBroItem.CreateGroup()
        .WithId(sensor.GroupId)
        .WithLabel(sensor.GroupName)
        .Build();
    }

    public MetricValue AsMetricValue()
    {
      return new MetricValue(sensor.Id, GetMetricValue(sensor));
    }
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
    return MetricTypeMap.GetValueOrDefault(sensorType, CoreMetricType.Numeric);
  }

  private static CoreCategory GetCategory(HardwareType hardwareType)
  {
    return HardwareCategoryMap.GetValueOrDefault(hardwareType, CoreCategory.Miscellaneous);
  }
}
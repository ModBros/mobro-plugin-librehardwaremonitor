using LibreHardwareMonitor.Hardware;

namespace MoBro.Plugin.LibreHardwareMonitor.Libre;

internal readonly record struct Sensor(
  string Id,
  string Name,
  float? Value,
  SensorType SensorType,
  HardwareType HardwareType,
  string GroupId,
  string GroupName
);
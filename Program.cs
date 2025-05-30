using System;
using System.Collections.Generic;
using MoBro.Plugin.LibreHardwareMonitor;
using MoBro.Plugin.SDK;
using Serilog.Events;

using var plugin = MoBroPluginBuilder
  .Create<Plugin>()
  .WithLogLevel(LogEventLevel.Debug)
  .WithSettings(new Dictionary<string, string>
  {
    ["cpu_enabled"] = "true",
    ["gpu_enabled"] = "true",
    ["ram_enabled"] = "true",
    ["motherboard_enabled"] = "true",
    ["hdd_enabled"] = "true",
    ["network_enabled"] = "true",
    ["controller_enabled"] = "true",
    ["psu_enabled"] = "true",
    ["battery_enabled"] = "true",
  })
  .Build();

Console.ReadLine();
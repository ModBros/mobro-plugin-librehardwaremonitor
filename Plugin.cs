using System;
using Microsoft.Extensions.Logging;
using MoBro.Plugin.LibreHardwareMonitor.Libre;
using MoBro.Plugin.SDK;
using MoBro.Plugin.SDK.Services;

namespace MoBro.Plugin.LibreHardwareMonitor;

public class Plugin : IMoBroPlugin, IDisposable
{
  private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(2);
  private const int DefaultUpdateFrequencyMs = 1000;
  private const int DefaultInitDelay = 0;

  private readonly IMoBroSettings _settings;
  private readonly IMoBroService _service;
  private readonly IMoBroScheduler _scheduler;

  private readonly ILogger _logger;

  private readonly Libre.LibreHardwareMonitor _libre;

  public Plugin(
    IMoBroSettings settings,
    IMoBroService service,
    IMoBroScheduler scheduler,
    ILogger logger
  )
  {
    _settings = settings;
    _service = service;
    _scheduler = scheduler;
    _logger = logger;
    _libre = new Libre.LibreHardwareMonitor(logger);
  }

  public void Init()
  {
    var initDelay = _settings.GetValue("init_delay", DefaultInitDelay);
    _scheduler.OneOff(InitLibre, TimeSpan.FromSeconds(initDelay));
  }

  private void InitLibre()
  {
    // check PawnIO status
    _service.SetDependencyStatus("pawnio", PawnIo.GetStatus());

    // update LibreHardwareMonitor according to settings
    _logger.LogInformation("Updating LibreHardwareMonitor settings");
    _libre.Update(_settings);

    // register groups and metrics
    _service.Register(_libre.GetMetricItems());

    // start polling metric values
    _logger.LogInformation("Starting metric polling");
    var updateFrequency = _settings.GetValue("update_frequency", DefaultUpdateFrequencyMs);
    _scheduler.Interval(UpdateMetricValues, TimeSpan.FromMilliseconds(updateFrequency), InitialDelay);
  }

  private void UpdateMetricValues()
  {
    _service.UpdateMetricValues(_libre.GetMetricValues());
  }

  public void Dispose()
  {
    _libre.Dispose();
  }
}
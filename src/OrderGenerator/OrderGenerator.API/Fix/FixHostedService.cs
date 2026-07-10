using OrderGenerator.Infrastructure.Exchange;
using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;

namespace OrderGenerator.API.Fix;

public class FixHostedService : IHostedService
{
    private readonly FixApplication _application;
    private readonly ILogger<FixHostedService> _logger;
    private SocketInitiator? _initiator;

    public FixHostedService(FixApplication application, ILogger<FixHostedService> logger)
    {
        _application = application;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = new SessionSettings("initiator.cfg");
        var storeFactory = new FileStoreFactory(settings);
        var logFactory = new FileLogFactory(settings);

        _initiator = new SocketInitiator(_application, storeFactory, settings, logFactory);
        _initiator.Start();

        _logger.LogInformation("FIX Initiator started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _initiator?.Stop();
        _logger.LogInformation("FIX Initiator stopped");
        return Task.CompletedTask;
    }
}
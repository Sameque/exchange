using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;

namespace OrderAccumulator.API.Fix;

public class FixAcceptorHostedService : IHostedService
{
    private readonly FixAcceptor _acceptor;
    private readonly ILogger<FixAcceptorHostedService> _logger;
    private ThreadedSocketAcceptor? _acceptorInstance;

    public FixAcceptorHostedService(FixAcceptor acceptor, ILogger<FixAcceptorHostedService> logger)
    {
        _acceptor = acceptor;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = new SessionSettings("config/client.cfg");
        var storeFactory = new FileStoreFactory(settings);
        var logFactory = new ScreenLogFactory(settings);

        _acceptorInstance = new ThreadedSocketAcceptor(_acceptor, storeFactory, settings, logFactory);
        _acceptorInstance.Start();

        _logger.LogInformation("FIX Acceptor started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _acceptorInstance?.Stop();
        _logger.LogInformation("FIX Acceptor stopped");
        return Task.CompletedTask;
    }
}
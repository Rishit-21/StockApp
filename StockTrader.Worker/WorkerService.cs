using Microsoft.Extensions.Options; // For IOptions
using StockTrader.Core.Models; // For Stock
using StockTrader.Data; // For StockTraderDbContext
using StockTrader.Service.Interfaces;
using Microsoft.Extensions.Hosting; // For BackgroundService
using Microsoft.Extensions.Logging; // For ILogger
using System; // For IServiceProvider, ArgumentNullException, DateTimeOffset, TimeSpan, Timer, Exception
using System.Linq; // For .Any()
using System.Threading; // For Timer, CancellationToken, Timeout
using System.Threading.Tasks; // For Task
using Microsoft.Extensions.DependencyInjection; // For IServiceScopeFactory / CreateScope
using System.Collections.Generic; // For List<string>


namespace StockTrader.Worker;

public class WorkerService : BackgroundService
{
    private readonly ILogger<WorkerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly WorkerSettings _workerSettings;

    // Timer specific for fetching live market data
    private Timer? _liveMarketDataTimer;
    // Timer specific for fetching sentiment data
    private Timer? _sentimentDataTimer;
    // Timer specific for generating trading tips
    private Timer? _tradingTipsTimer;


    public WorkerService(ILogger<WorkerService> logger, IServiceProvider serviceProvider, IOptions<WorkerSettings> workerSettings)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _workerSettings = workerSettings.Value ?? throw new ArgumentNullException(nameof(workerSettings));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WorkerService starting at: {time}", DateTimeOffset.Now);

        // Validate stock symbols
        if (_workerSettings.StockSymbolsToMonitor == null || !_workerSettings.StockSymbolsToMonitor.Any())
        {
            _logger.LogWarning("No stock symbols configured for monitoring. Worker will not perform stock-specific tasks effectively.");
        }
        else
        {
             _logger.LogInformation("Monitoring symbols: {symbols}", string.Join(", ", _workerSettings.StockSymbolsToMonitor));
        }


        // Setup timers for different tasks
        _liveMarketDataTimer = new Timer(
            DoFetchLiveMarketDataWork,
            null,
            TimeSpan.Zero, // Start immediately
            TimeSpan.FromSeconds(_workerSettings.FetchLiveMarketDataIntervalSeconds > 0 ? _workerSettings.FetchLiveMarketDataIntervalSeconds : 60) // Ensure positive interval
        );

        _sentimentDataTimer = new Timer(
            DoFetchSentimentDataWork,
            null,
            TimeSpan.FromSeconds(5), // Start after 5 seconds (to avoid initial burst)
            TimeSpan.FromSeconds(_workerSettings.FetchSentimentDataIntervalSeconds > 0 ? _workerSettings.FetchSentimentDataIntervalSeconds : 300) // Ensure positive interval
        );

        _tradingTipsTimer = new Timer(
            DoGenerateTradingTipsWork,
            null,
            TimeSpan.FromSeconds(10), // Start after 10 seconds
            TimeSpan.FromSeconds(_workerSettings.GenerateTipsIntervalSeconds > 0 ? _workerSettings.GenerateTipsIntervalSeconds : 600) // Ensure positive interval
        );
        
        stoppingToken.Register(() => {
             _logger.LogInformation("WorkerService stopping at: {time}", DateTimeOffset.Now);
            _liveMarketDataTimer?.Change(Timeout.Infinite, 0);
            _sentimentDataTimer?.Change(Timeout.Infinite, 0);
            _tradingTipsTimer?.Change(Timeout.Infinite, 0);
        });

        return Task.CompletedTask; // ExecuteAsync itself completes after setting up timers
    }

    private async void DoFetchLiveMarketDataWork(object? state)
    {
        _logger.LogInformation("Fetching live market data at: {time}", DateTimeOffset.Now);
        if (_workerSettings.StockSymbolsToMonitor == null || !_workerSettings.StockSymbolsToMonitor.Any()) return;

        using (var scope = _serviceProvider.CreateScope())
        {
            var liveMarketDataService = scope.ServiceProvider.GetRequiredService<ILiveMarketDataService>();
            try
            {
                await liveMarketDataService.FetchAndStoreLiveMarketDataAsync(_workerSettings.StockSymbolsToMonitor);
                _logger.LogInformation("Live market data fetch completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred fetching live market data.");
            }
        }
    }

    private async void DoFetchSentimentDataWork(object? state)
    {
        _logger.LogInformation("Fetching sentiment data at: {time}", DateTimeOffset.Now);
         if (_workerSettings.StockSymbolsToMonitor == null || !_workerSettings.StockSymbolsToMonitor.Any()) return;


        using (var scope = _serviceProvider.CreateScope())
        {
            var sentimentAnalysisService = scope.ServiceProvider.GetRequiredService<ISentimentAnalysisService>();
            try
            {
                // For sentiment, we might use the same symbols or broader keywords
                await sentimentAnalysisService.FetchAndStoreSentimentDataAsync(_workerSettings.StockSymbolsToMonitor);
                _logger.LogInformation("Sentiment data fetch completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred fetching sentiment data.");
            }
        }
    }

    private async void DoGenerateTradingTipsWork(object? state)
    {
        _logger.LogInformation("Generating trading tips at: {time}", DateTimeOffset.Now);
        if (_workerSettings.StockSymbolsToMonitor == null || !_workerSettings.StockSymbolsToMonitor.Any()) return;

        using (var scope = _serviceProvider.CreateScope())
        {
            var tradingTipService = scope.ServiceProvider.GetRequiredService<ITradingTipService>();
            try
            {
                foreach (var symbol in _workerSettings.StockSymbolsToMonitor)
                {
                    _logger.LogDebug($"Requesting tip generation for {symbol}.");
                    await tradingTipService.GenerateAndStoreTipsAsync(symbol);
                }
                _logger.LogInformation("Trading tips generation cycle completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred generating trading tips.");
            }
        }
    }
    
    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WorkerService stopping.");
        _liveMarketDataTimer?.Dispose();
        _sentimentDataTimer?.Dispose();
        _tradingTipsTimer?.Dispose();
        await base.StopAsync(stoppingToken);
    }
}

// Class to hold settings from appsettings.json
public class WorkerSettings
{
    public int FetchLiveMarketDataIntervalSeconds { get; set; } = 60;
    public int FetchSentimentDataIntervalSeconds { get; set; } = 300;
    public int GenerateTipsIntervalSeconds { get; set; } = 600;
    public List<string> StockSymbolsToMonitor { get; set; } = new List<string>();
}

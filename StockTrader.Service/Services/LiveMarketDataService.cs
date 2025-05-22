using Microsoft.Extensions.Logging;
using StockTrader.Core.Models;
using StockTrader.Data.Repositories;
using StockTrader.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection; // Required for GetService
using StockTrader.Data; // Required for the service locator hack to access _context

namespace StockTrader.Service.Services;

public class LiveMarketDataService : ILiveMarketDataService
{
    private readonly IRepository<Stock> _stockRepository;
    private readonly IRepository<LivePrice> _livePriceRepository;
    private readonly ILogger<LiveMarketDataService> _logger;
    private static readonly Random _random = new(); // For mock data generation

    public LiveMarketDataService(
        IRepository<Stock> stockRepository,
        IRepository<LivePrice> livePriceRepository,
        ILogger<LiveMarketDataService> logger)
    {
        _stockRepository = stockRepository ?? throw new ArgumentNullException(nameof(stockRepository));
        _livePriceRepository = livePriceRepository ?? throw new ArgumentNullException(nameof(livePriceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task FetchAndStoreLiveMarketDataAsync(IEnumerable<string> stockSymbols)
    {
        _logger.LogInformation($"Attempting to fetch live market data for symbols: {string.Join(", ", stockSymbols)}");
        var livePricesToAdd = new List<LivePrice>();

        foreach (var symbol in stockSymbols)
        {
            var stock = (await _stockRepository.FindAsync(s => s.Symbol == symbol)).FirstOrDefault();
            if (stock == null)
            {
                _logger.LogWarning($"Stock with symbol {symbol} not found. Skipping live data fetch for this stock.");
                continue;
            }

            // --- MOCK API CALL for Live Data ---
            // In a real application, you would call an external API here for each symbol.
            var livePriceData = GenerateMockLivePrice(stock.Id, symbol);
            // --- END MOCK API CALL ---

            if (livePriceData != null)
            {
                // Collect prices and add them later with AddRangeAsync for efficiency
                livePricesToAdd.Add(livePriceData);
                _logger.LogInformation($"Generated live price for {symbol}: Price {livePriceData.Price}, Volume {livePriceData.Volume}");
            }
            else
            {
                _logger.LogWarning($"No live data generated or fetched for {symbol}.");
            }
        }

        if (livePricesToAdd.Any())
        {
            await _livePriceRepository.AddRangeAsync(livePricesToAdd); // Use AddRangeAsync
            await _livePriceRepository.SaveChangesAsync(); // Single SaveChangesAsync call
            _logger.LogInformation($"Completed fetch and store cycle for live market data. Stored {livePricesToAdd.Count} price points.");
        }
        else
        {
            _logger.LogInformation("No new live market data to store in this cycle.");
        }
    }

    public async Task<LivePrice?> GetLatestLivePriceAsync(string stockSymbol)
    {
        var stock = (await _stockRepository.FindAsync(s => s.Symbol == stockSymbol)).FirstOrDefault();
        if (stock == null)
        {
            _logger.LogWarning($"Stock {stockSymbol} not found when trying to retrieve the latest live price.");
            return null;
        }

        var latestPrice = (await _livePriceRepository.FindAsync(
            lp => lp.StockId == stock.Id
        )).OrderByDescending(lp => lp.Timestamp).FirstOrDefault();

        return latestPrice;
    }

    private LivePrice? GenerateMockLivePrice(int stockId, string stockSymbol)
    {
        _logger.LogDebug($"Generating mock live price for {stockSymbol} (ID: {stockId}).");

        // Simulate that sometimes data might not be available
        if (_random.NextDouble() < 0.05) // 5% chance of no data
        {
            _logger.LogDebug($"Simulating no live data available for {stockSymbol}.");
            return null;
        }

        // Get the last historical price to base the live price on, or use a default
        // Using .Result here for simplicity in a mock, but avoid in real async code
        var lastHistorical = (_historicalPriceRepoServiceLocator.FindAsync(hp => hp.StockId == stockId).Result)
                             .OrderByDescending(hp => hp.Date)
                             .FirstOrDefault();

        decimal basePrice = lastHistorical?.Close ?? (decimal)(_random.NextDouble() * 1000 + 100);
        decimal priceChange = (decimal)((_random.NextDouble() - 0.45) * 0.1) * basePrice; // up to +/- 10% change from base, biased towards positive
        decimal currentPrice = basePrice + priceChange;
        if (currentPrice <= 0) currentPrice = 0.01m; // Price cannot be zero or negative

        return new LivePrice
        {
            StockId = stockId,
            Timestamp = DateTime.UtcNow,
            Price = Math.Round(currentPrice, 2),
            Volume = _random.Next(1000, 50000) // Smaller volume for live ticks typically
        };
    }

    // Need to add IRepository<HistoricalPrice> for GenerateMockLivePrice
    // This is a temporary workaround. Ideally, the live price generation logic
    // might be more sophisticated or not directly depend on historical repo here.
    // For the mock, it's acceptable.
    // IMPORTANT: This relies on _livePriceRepository being an instance of Repository<LivePrice>
    // and StockTraderDbContext being registered for IRepository<HistoricalPrice>.
    private IRepository<HistoricalPrice> _historicalPriceRepoServiceLocator
    {
        get
        {
            var concreteRepo = _livePriceRepository as Repository<LivePrice>;
            if (concreteRepo == null)
            {
                throw new InvalidOperationException($"The injected _livePriceRepository is not of concrete type Repository<LivePrice>, service locator hack cannot proceed.");
            }
            // Accessing _context which was made public for this hack.
            var histRepo = concreteRepo._context.GetService<IRepository<HistoricalPrice>>();
            return histRepo ?? throw new InvalidOperationException("HistoricalPrice repository not found via GetService. Ensure it's registered in DI.");
        }
    }
}

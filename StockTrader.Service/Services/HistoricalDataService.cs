using Microsoft.Extensions.Logging; // For logging
using StockTrader.Core.Models;
using StockTrader.Data.Repositories; // IRepository
using StockTrader.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StockTrader.Service.Services;

public class HistoricalDataService : IHistoricalDataService
{
    private readonly IRepository<Stock> _stockRepository;
    private readonly IRepository<HistoricalPrice> _historicalPriceRepository;
    private readonly ILogger<HistoricalDataService> _logger;

    public HistoricalDataService(
        IRepository<Stock> stockRepository,
        IRepository<HistoricalPrice> historicalPriceRepository,
        ILogger<HistoricalDataService> logger)
    {
        _stockRepository = stockRepository ?? throw new ArgumentNullException(nameof(stockRepository));
        _historicalPriceRepository = historicalPriceRepository ?? throw new ArgumentNullException(nameof(historicalPriceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task FetchAndStoreHistoricalDataAsync(string stockSymbol, int years)
    {
        _logger.LogInformation($"Attempting to fetch historical data for {stockSymbol} for the last {years} years.");

        var stock = (await _stockRepository.FindAsync(s => s.Symbol == stockSymbol)).FirstOrDefault();
        if (stock == null)
        {
            // Optionally, create the stock if it doesn't exist
            // For now, we'll assume stocks are pre-seeded or created elsewhere.
            _logger.LogWarning($"Stock with symbol {stockSymbol} not found.");
            // Or, create a new Stock:
            // stock = new Stock { Symbol = stockSymbol, Name = "Unknown - Fetched", Exchange = Core.Enums.Exchange.NSE }; // Example
            // await _stockRepository.AddAsync(stock);
            // await _stockRepository.SaveChangesAsync(); // SaveChanges for the new stock
            // _logger.LogInformation($"Created new stock entry for {stockSymbol}.");
            return; // Or handle as an error
        }

        // --- MOCK API CALL ---
        // In a real application, you would call an external API here.
        // For now, we simulate fetching data.
        var mockHistoricalData = GenerateMockHistoricalData(stock.Id, stockSymbol, years);
        // --- END MOCK API CALL ---

        if (mockHistoricalData.Any())
        {
            // Clear existing historical data for this stock for the fetched period
            // More sophisticated logic might be needed if only fetching partial updates,
            // but for a full N-year refresh, clearing is simpler.
            _logger.LogInformation($"Clearing existing historical data for {stock.Symbol} before adding new set.");
            var existingPrices = await _historicalPriceRepository.FindAsync(hp => hp.StockId == stock.Id); // Get all for the stock
            foreach (var oldPrice in existingPrices)
            {
                _historicalPriceRepository.Remove(oldPrice);
            }
            if (existingPrices.Any())
            {
                await _historicalPriceRepository.SaveChangesAsync(); // Commit deletions
                _logger.LogInformation($"Cleared {existingPrices.Count()} existing historical price points for {stock.Symbol}.");
            }

            await _historicalPriceRepository.AddRangeAsync(mockHistoricalData);
            await _historicalPriceRepository.SaveChangesAsync(); // Use SaveChangesAsync from the repository
            _logger.LogInformation($"Successfully fetched and stored {mockHistoricalData.Count()} historical price points for {stockSymbol}.");
        }
        else
        {
            _logger.LogInformation($"No historical data found/generated for {stockSymbol}.");
        }
    }

    public async Task<IEnumerable<HistoricalPrice>> GetHistoricalDataAsync(string stockSymbol, DateTime fromDate, DateTime toDate)
    {
        var stock = (await _stockRepository.FindAsync(s => s.Symbol == stockSymbol)).FirstOrDefault();
        if (stock == null)
        {
            _logger.LogWarning($"Stock {stockSymbol} not found when trying to retrieve historical data.");
            return Enumerable.Empty<HistoricalPrice>();
        }

        return await _historicalPriceRepository.FindAsync(
            hp => hp.StockId == stock.Id && hp.Date >= fromDate && hp.Date <= toDate
        );
    }

    private List<HistoricalPrice> GenerateMockHistoricalData(int stockId, string stockSymbol, int years)
    {
        _logger.LogDebug($"Generating mock historical data for {stockSymbol} (ID: {stockId}) for {years} years.");
        var prices = new List<HistoricalPrice>();
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddYears(-years);
        var random = new Random();

        for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
        {
            // Skip weekends for more realistic daily data
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                continue;
            }

            decimal open = (decimal)(random.NextDouble() * 1000 + 100); // Price between 100 and 1100
            decimal close = open + (decimal)((random.NextDouble() - 0.5) * 2 * (open * 0.05)); // +/- 5% change
            decimal high = Math.Max(open, close) + (decimal)(random.NextDouble() * (open * 0.03)); // Up to 3% higher
            decimal low = Math.Min(open, close) - (decimal)(random.NextDouble() * (open * 0.03)); // Up to 3% lower
            long volume = random.Next(100000, 10000000);

            // Ensure low <= open/close <= high
            if (low > open) low = open * 0.99m;
            if (low > close) low = close * 0.99m;
            if (high < open) high = open * 1.01m;
            if (high < close) high = close * 1.01m;


            prices.Add(new HistoricalPrice
            {
                StockId = stockId,
                Date = date,
                Open = Math.Round(open, 2),
                High = Math.Round(high, 2),
                Low = Math.Round(low, 2),
                Close = Math.Round(close, 2),
                Volume = volume
            });
        }
        _logger.LogDebug($"Generated {prices.Count} mock data points for {stockSymbol}.");
        return prices;
    }
}

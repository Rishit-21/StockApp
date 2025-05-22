using Microsoft.Extensions.Logging;
using StockTrader.Core.Models;
using StockTrader.Data.Repositories;
using StockTrader.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StockTrader.Service.Services;

public class TradingTipService : ITradingTipService
{
    private readonly ITradingStrategyEngine _strategyEngine;
    private readonly IRepository<TradingTip> _tipRepository;
    private readonly ILogger<TradingTipService> _logger;

    public TradingTipService(
        ITradingStrategyEngine strategyEngine,
        IRepository<TradingTip> tipRepository,
        ILogger<TradingTipService> logger)
    {
        _strategyEngine = strategyEngine ?? throw new ArgumentNullException(nameof(strategyEngine));
        _tipRepository = tipRepository ?? throw new ArgumentNullException(nameof(tipRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task GenerateAndStoreTipsAsync(string stockSymbol)
    {
        _logger.LogInformation($"Initiating tip generation for symbol: {stockSymbol}");
        var generatedTips = await _strategyEngine.GenerateTipsAsync(stockSymbol);

        if (generatedTips.Any())
        {
            // Optional: Add logic here to filter or prioritize tips before storing.
            // For now, we store all generated tips.
            await _tipRepository.AddRangeAsync(generatedTips);
            await _tipRepository.SaveChangesAsync();
            _logger.LogInformation($"Stored {generatedTips.Count()} new tips for {stockSymbol}.");
        }
        else
        {
            _logger.LogInformation($"No new tips were generated or stored for {stockSymbol}.");
        }
    }

    public async Task<IEnumerable<TradingTip>> GetRecentTipsAsync(string stockSymbol, int count = 10)
    {
        _logger.LogDebug($"Fetching last {count} tips for symbol: {stockSymbol}");
        var tips = await _tipRepository.FindAsync(t => t.StockSymbol == stockSymbol);
        return tips.OrderByDescending(t => t.Timestamp).Take(count);
    }

    public async Task<IEnumerable<TradingTip>> GetAllTipsAsync(int page = 1, int pageSize = 20)
    {
        _logger.LogDebug($"Fetching all tips, page: {page}, pageSize: {pageSize}");
        var allTips = await _tipRepository.GetAllAsync(); // This might need pagination in IRepository if dealing with huge data
        return allTips.OrderByDescending(t => t.Timestamp)
                      .Skip((page - 1) * pageSize)
                      .Take(pageSize);
    }
}

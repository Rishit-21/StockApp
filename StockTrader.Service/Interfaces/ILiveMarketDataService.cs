using StockTrader.Core.Models;

namespace StockTrader.Service.Interfaces;

public interface ILiveMarketDataService
{
    Task FetchAndStoreLiveMarketDataAsync(IEnumerable<string> stockSymbols);
    Task<LivePrice?> GetLatestLivePriceAsync(string stockSymbol);
}

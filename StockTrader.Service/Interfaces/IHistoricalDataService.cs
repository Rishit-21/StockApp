using StockTrader.Core.Models;

namespace StockTrader.Service.Interfaces;

public interface IHistoricalDataService
{
    Task FetchAndStoreHistoricalDataAsync(string stockSymbol, int years);
    Task<IEnumerable<HistoricalPrice>> GetHistoricalDataAsync(string stockSymbol, DateTime fromDate, DateTime toDate);
}

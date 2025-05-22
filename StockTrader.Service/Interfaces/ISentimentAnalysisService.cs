using StockTrader.Core.Models;

namespace StockTrader.Service.Interfaces;

public interface ISentimentAnalysisService
{
    Task FetchAndStoreSentimentDataAsync(IEnumerable<string> keywordsOrStockSymbols);
    Task<IEnumerable<SentimentData>> GetSentimentDataAsync(string? stockSymbol, DateTime fromDate, DateTime toDate);
}

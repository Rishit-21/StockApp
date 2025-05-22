using StockTrader.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StockTrader.Service.Interfaces;

public interface ITradingTipService
{
    Task GenerateAndStoreTipsAsync(string stockSymbol);
    Task<IEnumerable<TradingTip>> GetRecentTipsAsync(string stockSymbol, int count = 10);
    Task<IEnumerable<TradingTip>> GetAllTipsAsync(int page = 1, int pageSize = 20);
}

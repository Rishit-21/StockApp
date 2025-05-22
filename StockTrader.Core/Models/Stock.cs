using StockTrader.Core.Enums;

namespace StockTrader.Core.Models;

public class Stock
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Exchange Exchange { get; set; }
    public ICollection<HistoricalPrice> HistoricalPrices { get; set; } = new List<HistoricalPrice>();
    public ICollection<LivePrice> LivePrices { get; set; } = new List<LivePrice>();
}

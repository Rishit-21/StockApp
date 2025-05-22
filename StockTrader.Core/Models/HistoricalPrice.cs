using System;

namespace StockTrader.Core.Models;

public class HistoricalPrice
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public Stock? Stock { get; set; }
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}

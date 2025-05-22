using System;

namespace StockTrader.Core.Models;

public class LivePrice
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public Stock? Stock { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal Price { get; set; }
    public long Volume { get; set; }
}

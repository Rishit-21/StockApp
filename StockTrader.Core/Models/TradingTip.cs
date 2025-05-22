using System;
using StockTrader.Core.Enums;

namespace StockTrader.Core.Models;

public class TradingTip
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string StockSymbol { get; set; } = string.Empty;
    public TipType TipType { get; set; }
    public ActionType Action { get; set; } // Renamed from Action to ActionType
    public string Reason { get; set; } = string.Empty; // e.g., "High volume + positive global sentiment"
    public float ConfidenceScore { get; set; } // e.g., 0.0 to 1.0
}

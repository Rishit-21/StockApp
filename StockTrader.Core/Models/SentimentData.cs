using System;

namespace StockTrader.Core.Models;

public class SentimentData
{
    public int Id { get; set; }
    public string Source { get; set; } = string.Empty; // e.g., Twitter, NewsAPI
    public DateTime Timestamp { get; set; }
    public string Text { get; set; } = string.Empty; // Raw text
    public float SentimentScore { get; set; } // e.g., -1.0 (negative) to 1.0 (positive)
    public string StockSymbol { get; set; } = string.Empty; // Optional: if sentiment is specific to a stock
}

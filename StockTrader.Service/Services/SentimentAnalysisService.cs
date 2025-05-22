using Microsoft.Extensions.Logging;
using StockTrader.Core.Models;
using StockTrader.Data.Repositories;
using StockTrader.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StockTrader.Service.Services;

public class SentimentAnalysisService : ISentimentAnalysisService
{
    private readonly IRepository<SentimentData> _sentimentRepository;
    private readonly ILogger<SentimentAnalysisService> _logger;
    private static readonly Random _random = new();

    public SentimentAnalysisService(
        IRepository<SentimentData> sentimentRepository,
        ILogger<SentimentAnalysisService> logger)
    {
        _sentimentRepository = sentimentRepository ?? throw new ArgumentNullException(nameof(sentimentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task FetchAndStoreSentimentDataAsync(IEnumerable<string> keywordsOrStockSymbols)
    {
        _logger.LogInformation($"Attempting to fetch sentiment data for keywords/symbols: {string.Join(", ", keywordsOrStockSymbols)}");
        var sentimentEntries = new List<SentimentData>();

        foreach (var item in keywordsOrStockSymbols)
        {
            // --- MOCK API/NLP CALL for Sentiment Data ---
            // In a real application, you would query news APIs (e.g., NewsAPI.org),
            // social media (e.g., Twitter API), or use an NLP library/service here.
            var mockSentiments = GenerateMockSentimentData(item);
            // --- END MOCK API/NLP CALL ---

            if (mockSentiments.Any())
            {
                sentimentEntries.AddRange(mockSentiments);
                _logger.LogInformation($"Generated {mockSentiments.Count} sentiment entries for '{item}'.");
            }
            else
            {
                _logger.LogWarning($"No sentiment data generated or fetched for '{item}'.");
            }
        }

        if (sentimentEntries.Any())
        {
            await _sentimentRepository.AddRangeAsync(sentimentEntries);
            await _sentimentRepository.SaveChangesAsync();
            _logger.LogInformation($"Successfully stored {sentimentEntries.Count} sentiment data entries.");
        }
    }

    public async Task<IEnumerable<SentimentData>> GetSentimentDataAsync(string? stockSymbol, DateTime fromDate, DateTime toDate)
    {
        if (string.IsNullOrWhiteSpace(stockSymbol))
        {
            return await _sentimentRepository.FindAsync(
                sd => sd.Timestamp >= fromDate && sd.Timestamp <= toDate
            );
        }
        else
        {
            return await _sentimentRepository.FindAsync(
                sd => sd.StockSymbol == stockSymbol && sd.Timestamp >= fromDate && sd.Timestamp <= toDate
            );
        }
    }

    private List<SentimentData> GenerateMockSentimentData(string keywordOrSymbol)
    {
        _logger.LogDebug($"Generating mock sentiment data for '{keywordOrSymbol}'.");
        var sentiments = new List<SentimentData>();
        int numberOfEntries = _random.Next(1, 6); // Generate 1 to 5 mock sentiment entries

        string[] mockSources = { "Twitter", "Reddit", "NewsArticle" };
        string[] mockTexts = {
            $"{keywordOrSymbol} is looking very promising, great upward trend!",
            $"I'm feeling bearish about {keywordOrSymbol} after the recent announcement.",
            $"Not sure what to think about {keywordOrSymbol}, mixed signals everywhere.",
            $"Strong buy signals for {keywordOrSymbol} based on technicals.",
            $"Holding my {keywordOrSymbol} shares, expecting good news soon."
        };

        for (int i = 0; i < numberOfEntries; i++)
        {
            var source = mockSources[_random.Next(mockSources.Length)];
            var text = mockTexts[_random.Next(mockTexts.Length)];
            // Generate sentiment score: 60% chance positive, 30% negative, 10% neutral-ish
            float score;
            double r = _random.NextDouble();
            if (r < 0.6) score = (float)(_random.NextDouble() * 0.5 + 0.5); // 0.5 to 1.0
            else if (r < 0.9) score = (float)(_random.NextDouble() * -0.5 - 0.5); // -0.5 to -1.0
            else score = (float)(_random.NextDouble() * 0.4 - 0.2); // -0.2 to 0.2


            sentiments.Add(new SentimentData
            {
                Source = source,
                Timestamp = DateTime.UtcNow.AddMinutes(-_random.Next(0, 120)), // Within the last 2 hours
                Text = text,
                SentimentScore = (float)Math.Round(score, 2),
                StockSymbol = keywordOrSymbol // Assuming keyword is the stock symbol for this mock
            });
        }
        _logger.LogDebug($"Generated {sentiments.Count} mock sentiment data points for '{keywordOrSymbol}'.");
        return sentiments;
    }
}

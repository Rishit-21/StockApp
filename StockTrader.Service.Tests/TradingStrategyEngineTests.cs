using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using StockTrader.Core.Models;
using StockTrader.Core.Enums;
using StockTrader.Service.Interfaces;
using StockTrader.Service.Services; // Ensure this is correct
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StockTrader.Service.Tests;

public class TradingStrategyEngineTests
{
    private readonly Mock<IHistoricalDataService> _mockHistoricalDataService;
    private readonly Mock<ILiveMarketDataService> _mockLiveMarketDataService;
    private readonly Mock<ISentimentAnalysisService> _mockSentimentAnalysisService;
    private readonly Mock<ILogger<TradingStrategyEngine>> _mockLogger;
    private readonly TradingStrategyEngine _sut;

    public TradingStrategyEngineTests()
    {
        _mockHistoricalDataService = new Mock<IHistoricalDataService>();
        _mockLiveMarketDataService = new Mock<ILiveMarketDataService>();
        _mockSentimentAnalysisService = new Mock<ISentimentAnalysisService>();
        _mockLogger = new Mock<ILogger<TradingStrategyEngine>>();

        _sut = new TradingStrategyEngine(
            _mockHistoricalDataService.Object,
            _mockLiveMarketDataService.Object,
            _mockSentimentAnalysisService.Object,
            _mockLogger.Object
        );
    }

    private void SetupMocks(
        LivePrice? livePrice,
        List<HistoricalPrice> historicalPrices,
        List<SentimentData> sentimentData)
    {
        _mockLiveMarketDataService.Setup(s => s.GetLatestLivePriceAsync(It.IsAny<string>()))
                                  .ReturnsAsync(livePrice);
        _mockHistoricalDataService.Setup(s => s.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                                   .ReturnsAsync(historicalPrices);
        _mockSentimentAnalysisService.Setup(s => s.GetSentimentDataAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                                      .ReturnsAsync(sentimentData);
    }

    [Fact]
    public async Task GenerateTipsAsync_NoLivePrice_ShouldReturnNoTips()
    {
        // Arrange
        SetupMocks(null, new List<HistoricalPrice>(), new List<SentimentData>());

        // Act
        var tips = await _sut.GenerateTipsAsync("TEST");

        // Assert
        tips.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateTipsAsync_Rule1_HighVolumePositiveSentimentPriceIncrease_ShouldGenerateIntradayBuyTip()
    {
        // Arrange
        var livePrice = new LivePrice { Price = 105m, Volume = 6000000 }; // High volume
        var historicalPrices = new List<HistoricalPrice>
        {
            new HistoricalPrice { Date = DateTime.UtcNow.AddDays(-5), Close = 95m }, // Older price
            new HistoricalPrice { Date = DateTime.UtcNow.AddDays(-4), Close = 98m },
            new HistoricalPrice { Date = DateTime.UtcNow.AddDays(-3), Close = 100m }  // Price increased vs this
        };
        var sentimentData = new List<SentimentData> { new SentimentData { SentimentScore = 0.7f } }; // Positive sentiment
        SetupMocks(livePrice, historicalPrices, sentimentData);

        // Act
        var tips = await _sut.GenerateTipsAsync("TEST");

        // Assert
        tips.Should().ContainSingle();
        var tip = tips.First();
        tip.TipType.Should().Be(TipType.Intraday);
        tip.Action.Should().Be(ActionType.Buy);
        tip.Reason.Should().Contain("High volume");
        tip.Reason.Should().Contain("positive sentiment");
        tip.Reason.Should().Contain("upward price trend");
        tip.ConfidenceScore.Should().Be(0.7f);
    }

    [Fact]
    public async Task GenerateTipsAsync_Rule2_SignificantPriceDropNegativeSentiment_ShouldGenerateSwingSellTip()
    {
        // Arrange
        var livePrice = new LivePrice { Price = 85m, Volume = 1000000 };
        var historicalPrices = new List<HistoricalPrice>
        {
            new HistoricalPrice { Date = DateTime.UtcNow.AddDays(-10), Close = 100m }, // Week old price (approx)
            new HistoricalPrice { Date = DateTime.UtcNow.AddDays(-5), Close = 95m },
             new HistoricalPrice { Date = DateTime.UtcNow.AddDays(-6), Close = 95m },
              new HistoricalPrice { Date = DateTime.UtcNow.AddDays(-7), Close = 95m },
               new HistoricalPrice { Date = DateTime.UtcNow.AddDays(-8), Close = 95m },
                new HistoricalPrice { Date = DateTime.UtcNow.AddDays(-9), Close = 95m }
        }; // Ensure Count > 5 for Skip(5)
        var sentimentData = new List<SentimentData> { new SentimentData { SentimentScore = -0.5f } }; // Negative sentiment
        SetupMocks(livePrice, historicalPrices, sentimentData);

        // Act
        var tips = await _sut.GenerateTipsAsync("TEST");

        // Assert
        tips.Should().ContainSingle(t => t.TipType == TipType.Swing && t.Action == ActionType.Sell);
        var tip = tips.First(t => t.TipType == TipType.Swing && t.Action == ActionType.Sell);
        tip.Reason.Should().Contain("Significant price drop");
        tip.Reason.Should().Contain("negative sentiment");
        tip.ConfidenceScore.Should().Be(0.65f);
    }
    
    [Fact]
    public async Task GenerateTipsAsync_Rule3_HighVolatilityPositiveSentiment_ShouldGenerateOptionsBuyCallTip()
    {
        // Arrange
        var livePrice = new LivePrice { Price = 150m, Volume = 1000000 };
        var historicalPrices = new List<HistoricalPrice> // Create volatile data
        {
            new HistoricalPrice { Date = DateTime.UtcNow.AddDays(-6), Close = 100m },
            new HistoricalPrice { Date = DateTime.UtcNow.AddDays(-5), Close = 110m },
            new HistoricalPrice { Date = DateTime.UtcNow.AddDays(-4), Close = 105m },
            new HistoricalPrice { Date = DateTime.UtcNow.AddDays(-3), Close = 120m },
            new HistoricalPrice { Date = DateTime.UtcNow.AddDays(-2), Close = 110m },
            new HistoricalPrice { Date = DateTime.UtcNow.AddDays(-1), Close = 130m } 
        }; // std dev should be high enough
        var sentimentData = new List<SentimentData> { new SentimentData { SentimentScore = 0.4f } }; // Positive sentiment
        SetupMocks(livePrice, historicalPrices, sentimentData);

        // Act
        var tips = await _sut.GenerateTipsAsync("TEST");
        
        // Assert
        tips.Should().NotBeEmpty();
        var optionsTip = tips.FirstOrDefault(t => t.TipType == TipType.Options && t.Action == ActionType.Buy);
        optionsTip.Should().NotBeNull();
        optionsTip.Reason.Should().Contain("High relative volatility");
        optionsTip.Reason.Should().Contain("positive sentiment");
        optionsTip.Reason.Should().Contain("Consider Call Option");
        optionsTip.ConfidenceScore.Should().Be(0.6f);
    }


    [Fact]
    public async Task GenerateTipsAsync_NoRulesMet_ShouldReturnNoTips()
    {
        // Arrange
        var livePrice = new LivePrice { Price = 100m, Volume = 100000 }; // Low volume
        var historicalPrices = new List<HistoricalPrice>
        {
            new HistoricalPrice { Date = DateTime.UtcNow.AddDays(-5), Close = 99m }, // No significant trend
            new HistoricalPrice { Date = DateTime.UtcNow.AddDays(-3), Close = 100m }
        };
        var sentimentData = new List<SentimentData> { new SentimentData { SentimentScore = 0.1f } }; // Neutral sentiment
        SetupMocks(livePrice, historicalPrices, sentimentData);

        // Act
        var tips = await _sut.GenerateTipsAsync("TEST");

        // Assert
        tips.Should().BeEmpty();
    }
}

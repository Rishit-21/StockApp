using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using StockTrader.Core.Models;
using StockTrader.Core.Enums;
using StockTrader.Data.Repositories;
using StockTrader.Service.Interfaces; // For ITradingStrategyEngine
using StockTrader.Service.Services;   // For TradingTipService
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace StockTrader.Service.Tests;

public class TradingTipServiceTests
{
    private readonly Mock<ITradingStrategyEngine> _mockStrategyEngine;
    private readonly Mock<IRepository<TradingTip>> _mockTipRepository;
    private readonly Mock<ILogger<TradingTipService>> _mockLogger;
    private readonly TradingTipService _sut;

    public TradingTipServiceTests()
    {
        _mockStrategyEngine = new Mock<ITradingStrategyEngine>();
        _mockTipRepository = new Mock<IRepository<TradingTip>>();
        _mockLogger = new Mock<ILogger<TradingTipService>>();

        _sut = new TradingTipService(
            _mockStrategyEngine.Object,
            _mockTipRepository.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GenerateAndStoreTipsAsync_EngineReturnsTips_ShouldStoreTips()
    {
        // Arrange
        string symbol = "TEST";
        var tipsFromEngine = new List<TradingTip>
        {
            new TradingTip { StockSymbol = symbol, Action = ActionType.Buy, TipType = TipType.Intraday, Reason = "R1" }
        };
        _mockStrategyEngine.Setup(e => e.GenerateTipsAsync(symbol)).ReturnsAsync(tipsFromEngine);
        _mockTipRepository.Setup(r => r.AddRangeAsync(tipsFromEngine)).Returns(Task.CompletedTask);
        _mockTipRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _sut.GenerateAndStoreTipsAsync(symbol);

        // Assert
        _mockStrategyEngine.Verify(e => e.GenerateTipsAsync(symbol), Times.Once);
        _mockTipRepository.Verify(r => r.AddRangeAsync(tipsFromEngine), Times.Once);
        _mockTipRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GenerateAndStoreTipsAsync_EngineReturnsNoTips_ShouldNotStore()
    {
        // Arrange
        string symbol = "TEST";
        var noTipsFromEngine = new List<TradingTip>(); // Empty list
        _mockStrategyEngine.Setup(e => e.GenerateTipsAsync(symbol)).ReturnsAsync(noTipsFromEngine);

        // Act
        await _sut.GenerateAndStoreTipsAsync(symbol);

        // Assert
        _mockStrategyEngine.Verify(e => e.GenerateTipsAsync(symbol), Times.Once);
        _mockTipRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<TradingTip>>()), Times.Never);
        _mockTipRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetRecentTipsAsync_ShouldReturnOrderedTipsFromRepository()
    {
        // Arrange
        string symbol = "TEST";
        int count = 2;
        var allTipsForSymbol = new List<TradingTip>
        {
            new TradingTip { StockSymbol = symbol, Timestamp = DateTime.UtcNow.AddMinutes(-10) },
            new TradingTip { StockSymbol = symbol, Timestamp = DateTime.UtcNow }, // Most recent
            new TradingTip { StockSymbol = symbol, Timestamp = DateTime.UtcNow.AddMinutes(-5) }
        };
        _mockTipRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<TradingTip, bool>>>()))
                          .ReturnsAsync(allTipsForSymbol);
        
        var expectedTips = allTipsForSymbol.OrderByDescending(t => t.Timestamp).Take(count).ToList();

        // Act
        var result = await _sut.GetRecentTipsAsync(symbol, count);

        // Assert
        result.Should().BeEquivalentTo(expectedTips, options => options.WithStrictOrdering());
        _mockTipRepository.Verify(r => r.FindAsync(It.Is<Expression<Func<TradingTip, bool>>>(
            expr => CheckTipExpression(expr, symbol) // Basic check for symbol
        )), Times.Once);
    }

    [Fact]
    public async Task GetAllTipsAsync_ShouldReturnPaginatedTipsFromRepository()
    {
        // Arrange
        int page = 1;
        int pageSize = 2;
        var allTips = new List<TradingTip>
        {
            new TradingTip { Timestamp = DateTime.UtcNow.AddMinutes(-1) },
            new TradingTip { Timestamp = DateTime.UtcNow.AddMinutes(-2) },
            new TradingTip { Timestamp = DateTime.UtcNow.AddMinutes(-3) }
        };
         _mockTipRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(allTips); // Assuming GetAllAsync exists and works for this test

        var expectedTips = allTips.OrderByDescending(t => t.Timestamp)
                                  .Skip((page - 1) * pageSize)
                                  .Take(pageSize).ToList();
        // Act
        var result = await _sut.GetAllTipsAsync(page, pageSize);

        // Assert
        result.Should().BeEquivalentTo(expectedTips, options => options.WithStrictOrdering());
    }
    
    private bool CheckTipExpression(Expression<Func<TradingTip, bool>> expression, string expectedSymbol)
    {
        // Simplified check, real expression checking is complex.
        string exprStr = expression.ToString();
        return exprStr.Contains($"t.StockSymbol == \"{expectedSymbol}\"");
    }
}

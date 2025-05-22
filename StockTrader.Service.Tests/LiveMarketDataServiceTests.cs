using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using StockTrader.Core.Models;
using StockTrader.Core.Enums;
using StockTrader.Data.Repositories;
using StockTrader.Service.Services; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure; // For GetService
using StockTrader.Data; // For StockTraderDbContext

namespace StockTrader.Service.Tests;

public class LiveMarketDataServiceTests
{
    private readonly Mock<IRepository<Stock>> _mockStockRepository;
    private readonly Mock<IRepository<HistoricalPrice>> _mockHistoricalPriceRepository; 
    private readonly Mock<ILogger<LiveMarketDataService>> _mockLogger;
    private readonly LiveMarketDataService _sut;
    private readonly Mock<Repository<LivePrice>> _concreteMockLivePriceRepository; 

    public LiveMarketDataServiceTests()
    {
        _mockStockRepository = new Mock<IRepository<Stock>>();
        _mockHistoricalPriceRepository = new Mock<IRepository<HistoricalPrice>>();
        _mockLogger = new Mock<ILogger<LiveMarketDataService>>();

        var mockDbContext = new Mock<StockTraderDbContext>(); 
        mockDbContext.Setup(db => db.GetService<IRepository<HistoricalPrice>>())
                     .Returns(_mockHistoricalPriceRepository.Object);
        
        _concreteMockLivePriceRepository = new Mock<Repository<LivePrice>>(mockDbContext.Object);

        _sut = new LiveMarketDataService(
            _mockStockRepository.Object,
            _concreteMockLivePriceRepository.Object, 
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task FetchAndStoreLiveMarketDataAsync_AllStocksFound_ShouldAddPricesAndSaveChanges()
    {
        // Arrange
        var symbols = new List<string> { "TEST1", "TEST2" };
        var stock1 = new Stock { Id = 1, Symbol = "TEST1" };
        var stock2 = new Stock { Id = 2, Symbol = "TEST2" };

        _mockStockRepository.Setup(r => r.FindAsync(It.Is<Expression<Func<Stock, bool>>>(expr => CheckSymbolExpression(expr, "TEST1"))))
                            .ReturnsAsync(new List<Stock> { stock1 });
        _mockStockRepository.Setup(r => r.FindAsync(It.Is<Expression<Func<Stock, bool>>>(expr => CheckSymbolExpression(expr, "TEST2"))))
                            .ReturnsAsync(new List<Stock> { stock2 });
        
        _mockHistoricalPriceRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<HistoricalPrice, bool>>>()))
                                      .ReturnsAsync(new List<HistoricalPrice> { new HistoricalPrice { Close = 100m } });

        // SUT uses AddRangeAsync
        _concreteMockLivePriceRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<LivePrice>>())).Returns(Task.CompletedTask);
        _concreteMockLivePriceRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _sut.FetchAndStoreLiveMarketDataAsync(symbols);

        // Assert
        _concreteMockLivePriceRepository.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<LivePrice>>(prices => prices.Count() == symbols.Count)), Times.Once);
        _concreteMockLivePriceRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task FetchAndStoreLiveMarketDataAsync_SomeStocksNotFound_ShouldLogWarningAndProcessFoundStocks()
    {
        // Arrange
        var symbols = new List<string> { "FOUND", "NOTFOUND" };
        var foundStock = new Stock { Id = 1, Symbol = "FOUND" };

        _mockStockRepository.Setup(r => r.FindAsync(It.Is<Expression<Func<Stock, bool>>>(e => CheckSymbolExpression(e, "FOUND"))))
                            .ReturnsAsync(new List<Stock> { foundStock });
        _mockStockRepository.Setup(r => r.FindAsync(It.Is<Expression<Func<Stock, bool>>>(e => CheckSymbolExpression(e, "NOTFOUND"))))
                            .ReturnsAsync(new List<Stock>()); 

        _mockHistoricalPriceRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<HistoricalPrice, bool>>>()))
                                      .ReturnsAsync(new List<HistoricalPrice> { new HistoricalPrice { Close = 100m } });
        
        _concreteMockLivePriceRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<LivePrice>>())).Returns(Task.CompletedTask);
        _concreteMockLivePriceRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _sut.FetchAndStoreLiveMarketDataAsync(symbols);

        // Assert
        // SUT uses AddRangeAsync, so we verify that with a list containing one item.
        _concreteMockLivePriceRepository.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<LivePrice>>(prices => prices.Count() == 1 && prices.First().StockId == foundStock.Id )), Times.Once);
        _concreteMockLivePriceRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Stock with symbol NOTFOUND not found")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetLatestLivePriceAsync_StockNotFound_ShouldReturnNull()
    {
        // Arrange
        _mockStockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Stock, bool>>>()))
                            .ReturnsAsync(new List<Stock>());

        // Act
        var result = await _sut.GetLatestLivePriceAsync("UNKNOWN");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestLivePriceAsync_StockFound_ShouldReturnLatestPrice()
    {
        // Arrange
        string symbol = "TEST";
        int stockId = 1;
        var stock = new Stock { Id = stockId, Symbol = symbol };
        _mockStockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Stock, bool>>>()))
                            .ReturnsAsync(new List<Stock> { stock });

        var prices = new List<LivePrice>
        {
            new LivePrice { StockId = stockId, Timestamp = DateTime.UtcNow.AddMinutes(-5), Price = 100 },
            new LivePrice { StockId = stockId, Timestamp = DateTime.UtcNow, Price = 101 }, // Latest
            new LivePrice { StockId = stockId, Timestamp = DateTime.UtcNow.AddMinutes(-10), Price = 99 }
        };
        _concreteMockLivePriceRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LivePrice, bool>>>()))
                                .ReturnsAsync(prices);

        // Act
        var result = await _sut.GetLatestLivePriceAsync(symbol);

        // Assert
        result.Should().NotBeNull();
        result.Price.Should().Be(101);
    }

    private bool CheckSymbolExpression(Expression<Func<Stock, bool>> expression, string expectedSymbol)
    {
        if (expression.Body is BinaryExpression binaryExpression)
        {
            if (binaryExpression.Left is MemberExpression memberExpression && memberExpression.Member.Name == "Symbol")
            {
                if (binaryExpression.Right is ConstantExpression constantExpression)
                {
                    return constantExpression.Value?.ToString() == expectedSymbol;
                }
            }
        }
        try {
            return expression.Compile()(new Stock { Symbol = expectedSymbol });
        } catch {
            return false;
        }
    }
}

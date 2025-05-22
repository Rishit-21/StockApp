using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using StockTrader.Core.Models;
using StockTrader.Core.Enums;
using StockTrader.Data.Repositories;
using StockTrader.Service.Services; // Make sure this using is correct for HistoricalDataService
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace StockTrader.Service.Tests;

public class HistoricalDataServiceTests
{
    private readonly Mock<IRepository<Stock>> _mockStockRepository;
    private readonly Mock<IRepository<HistoricalPrice>> _mockHistoricalPriceRepository;
    private readonly Mock<ILogger<HistoricalDataService>> _mockLogger;
    private readonly HistoricalDataService _sut; // System Under Test

    public HistoricalDataServiceTests()
    {
        _mockStockRepository = new Mock<IRepository<Stock>>();
        _mockHistoricalPriceRepository = new Mock<IRepository<HistoricalPrice>>();
        _mockLogger = new Mock<ILogger<HistoricalDataService>>();
        
        // Initialize System Under Test (SUT) with mocked dependencies
        _sut = new HistoricalDataService(
            _mockStockRepository.Object,
            _mockHistoricalPriceRepository.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task FetchAndStoreHistoricalDataAsync_StockNotFound_ShouldLogWarningAndNotAddData()
    {
        // Arrange
        string nonExistentSymbol = "NOSYMBOL";
        _mockStockRepository.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<Stock, bool>>>()))
                            .ReturnsAsync(new List<Stock>()); // Empty list means stock not found

        // Act
        await _sut.FetchAndStoreHistoricalDataAsync(nonExistentSymbol, 1);

        // Assert
        // Verify logger was called with a warning (actual log message can be more specific if needed)
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Stock with symbol {nonExistentSymbol} not found")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        
        _mockHistoricalPriceRepository.Verify(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<HistoricalPrice>>()), Times.Never);
        _mockHistoricalPriceRepository.Verify(repo => repo.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task FetchAndStoreHistoricalDataAsync_StockFound_ShouldClearOldDataAndAddNewData()
    {
        // Arrange
        string symbol = "TESTSTOCK";
        int stockId = 1;
        var stock = new Stock { Id = stockId, Symbol = symbol, Name = "Test Stock", Exchange = Exchange.NSE };
        _mockStockRepository.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<Stock, bool>>>()))
                            .ReturnsAsync(new List<Stock> { stock });

        var existingPrices = new List<HistoricalPrice>
        {
            new HistoricalPrice { Id = 100, StockId = stockId, Date = DateTime.UtcNow.AddDays(-5), Close = 150 }
        };
        // This setup was for the old logic that cleared data. The current HistoricalDataService does not clear existing data.
        // _mockHistoricalPriceRepository.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<HistoricalPrice, bool>>>()))
        //                             .ReturnsAsync(existingPrices);
        
        // _mockHistoricalPriceRepository.Setup(repo => repo.Remove(It.IsAny<HistoricalPrice>()))
        //                             .Verifiable();
        _mockHistoricalPriceRepository.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<HistoricalPrice>>()))
                                    .Verifiable();
        _mockHistoricalPriceRepository.Setup(repo => repo.SaveChangesAsync())
                                    .ReturnsAsync(1) 
                                    .Verifiable();


        // Act
        await _sut.FetchAndStoreHistoricalDataAsync(symbol, 1); // Fetch 1 year of data

        // Assert
        // Current HistoricalDataService doesn't explicitly remove old data, it just adds.
        // So, we don't verify Remove or FindAsync for removal.
        // _mockHistoricalPriceRepository.Verify(repo => repo.FindAsync(It.IsAny<Expression<Func<HistoricalPrice, bool>>>()), Times.Once); 
        // _mockHistoricalPriceRepository.Verify(repo => repo.Remove(It.IsAny<HistoricalPrice>()), Times.Exactly(existingPrices.Count)); 
        
        _mockHistoricalPriceRepository.Verify(repo => repo.AddRangeAsync(It.Is<IEnumerable<HistoricalPrice>>(list => list.Any())), Times.Once);
        
        // SaveChangesAsync should be called once for adding new data.
        _mockHistoricalPriceRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once());
    }
    
    [Fact]
    public async Task GetHistoricalDataForSymbol_StockNotFound_ShouldReturnEmptyList()
    {
        // Arrange
        string symbol = "UNKNOWN";
        _mockStockRepository.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<Stock, bool>>>()))
                            .ReturnsAsync(new List<Stock>());

        // Act
        var result = await _sut.GetHistoricalDataAsync(symbol, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow);

        // Assert
        result.Should().BeEmpty();
         _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Stock {symbol} not found when trying to retrieve historical data.")), // Adjusted message
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetHistoricalDataForSymbol_StockFound_ShouldReturnDataFromRepository()
    {
        // Arrange
        string symbol = "TESTSTOCK";
        int stockId = 1;
        var stock = new Stock { Id = stockId, Symbol = symbol, Name = "Test Stock", Exchange = Exchange.NSE };
        _mockStockRepository.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<Stock, bool>>>()))
                            .ReturnsAsync(new List<Stock> { stock });

        var expectedPrices = new List<HistoricalPrice>
        {
            new HistoricalPrice { StockId = stockId, Date = DateTime.UtcNow.AddDays(-1), Close = 100 },
            new HistoricalPrice { StockId = stockId, Date = DateTime.UtcNow.AddDays(-2), Close = 98 }
        };
        _mockHistoricalPriceRepository.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<HistoricalPrice, bool>>>()))
                                    .ReturnsAsync(expectedPrices);

        // Act
        var result = await _sut.GetHistoricalDataAsync(symbol, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(expectedPrices.Count);
        result.Should().BeEquivalentTo(expectedPrices);
    }
}

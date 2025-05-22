using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using StockTrader.Core.Models;
using StockTrader.Data.Repositories;
using StockTrader.Service.Services; // Ensure this is the correct namespace
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace StockTrader.Service.Tests;

public class SentimentAnalysisServiceTests
{
    private readonly Mock<IRepository<SentimentData>> _mockSentimentRepository;
    private readonly Mock<ILogger<SentimentAnalysisService>> _mockLogger;
    private readonly SentimentAnalysisService _sut;

    public SentimentAnalysisServiceTests()
    {
        _mockSentimentRepository = new Mock<IRepository<SentimentData>>();
        _mockLogger = new Mock<ILogger<SentimentAnalysisService>>();
        
        _sut = new SentimentAnalysisService(
            _mockSentimentRepository.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task FetchAndStoreSentimentDataAsync_ShouldAddGeneratedSentimentsAndSaveChanges()
    {
        // Arrange
        var keywords = new List<string> { "STOCKA", "STOCKB" };
        _mockSentimentRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<SentimentData>>()))
                                .Returns(Task.CompletedTask);
        _mockSentimentRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _sut.FetchAndStoreSentimentDataAsync(keywords);

        // Assert
        _mockSentimentRepository.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<SentimentData>>(list => list.Any() && list.Count() >= keywords.Count)), Times.Once);
        // Check that each keyword resulted in at least one sentiment entry (mock generation detail)
        // The mock generates 1-5 entries per keyword. So total entries should be >= keywords.Count and <= keywords.Count * 5
        _mockSentimentRepository.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<SentimentData>>(
            list => list.All(s => keywords.Contains(s.StockSymbol.ToUpper())) && // Check if symbols match
                    list.Count() >= keywords.Count && list.Count() <= keywords.Count * 5 
        )), Times.Once);
        _mockSentimentRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSentimentDataAsync_WithStockSymbol_ShouldCallRepositoryWithCorrectFilter()
    {
        // Arrange
        string symbol = "TESTSYM";
        DateTime fromDate = DateTime.UtcNow.AddDays(-7);
        DateTime toDate = DateTime.UtcNow;
        var expectedSentiments = new List<SentimentData> { new SentimentData { StockSymbol = symbol, SentimentScore = 0.5f } };

        _mockSentimentRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<SentimentData, bool>>>()))
                                .ReturnsAsync(expectedSentiments)
                                .Verifiable();
        // Act
        var result = await _sut.GetSentimentDataAsync(symbol, fromDate, toDate);

        // Assert
        result.Should().BeEquivalentTo(expectedSentiments);
        _mockSentimentRepository.Verify(r => r.FindAsync(It.Is<Expression<Func<SentimentData, bool>>>(
            expr => CheckSentimentExpression(expr, symbol, fromDate, toDate) 
        )), Times.Once);
    }

    [Fact]
    public async Task GetSentimentDataAsync_WithoutStockSymbol_ShouldCallRepositoryWithDateFilterOnly()
    {
        // Arrange
        DateTime fromDate = DateTime.UtcNow.AddDays(-7);
        DateTime toDate = DateTime.UtcNow;
        var expectedSentiments = new List<SentimentData> { new SentimentData { SentimentScore = 0.5f } };
         _mockSentimentRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<SentimentData, bool>>>()))
                                .ReturnsAsync(expectedSentiments)
                                .Verifiable();

        // Act
        var result = await _sut.GetSentimentDataAsync(null, fromDate, toDate);

        // Assert
        result.Should().BeEquivalentTo(expectedSentiments);
        _mockSentimentRepository.Verify(r => r.FindAsync(It.Is<Expression<Func<SentimentData, bool>>>(
            expr => CheckSentimentExpression(expr, null, fromDate, toDate)
        )), Times.Once);
    }

    // Helper method to check the expression passed to FindAsync
    // This is a bit complex due to expression tree inspection.
    // A simpler approach for tests might be to just trust the SUT calls the repo and the repo works,
    // and only verify the interaction (i.e., FindAsync was called).
    // However, this demonstrates deeper verification if needed.
    private bool CheckSentimentExpression(Expression<Func<SentimentData, bool>> expression, 
                                          string expectedSymbol, DateTime expectedFromDate, DateTime expectedToDate)
    {
        // This simplified check focuses on string representation for brevity.
        // Robust expression tree parsing is complex and often overkill for unit tests
        // if the primary goal is to ensure the service attempts to filter.
        string exprStr = expression.ToString().ToUpper(); // Convert to upper for case-insensitive symbol check

        bool symbolMatch = true; // Assume true if no symbol filter expected
        if (expectedSymbol != null)
        {
            // Check if the expression string contains the expected symbol part.
            // This is a loose check. e.g., "sd.StockSymbol == \"TESTSYM\""
            symbolMatch = exprStr.Contains($"STOCKSYMBOL == \"{expectedSymbol.ToUpper()}\"");
        }
        else
        {
            // Ensure no stock symbol filter is present if not expected
            symbolMatch = !exprStr.Contains("STOCKSYMBOL");
        }

        // Check for date components (simplified)
        // Assumes parameters are named 'fromDate' and 'toDate' in the compiled expression or similar structure
        bool fromDateMatch = exprStr.Contains("TIMESTAMP >= ");
        bool toDateMatch = exprStr.Contains("TIMESTAMP <= ");
        
        // This is a basic check. For more precise validation, you would need to
        // compile the expression with sample data or use a more sophisticated ExpressionVisitor.
        // For many tests, simply verifying that FindAsync was called might be sufficient,
        // relying on integration tests for the full predicate logic.
        return symbolMatch && fromDateMatch && toDateMatch;
    }
}

using Xunit;
using FluentAssertions;
using StockTrader.Core.Models; // For TradingTip
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json; // For ReadFromJsonAsync
using System.Threading.Tasks;
using System.Linq; // For LINQ methods like Any()

namespace StockTrader.Api.Tests;

public class TradingTipsControllerTests : IClassFixture<StockTraderApiFactory>
{
    private readonly HttpClient _client;
    private readonly StockTraderApiFactory _factory; // If needed for direct interaction

    public TradingTipsControllerTests(StockTraderApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRecentTips_ValidSymbol_ShouldReturnSeededTips()
    {
        // Arrange
        string symbol = "TESTMSFT"; // This stock is seeded with a tip
        int count = 5;

        // Act
        var response = await _client.GetAsync($"/api/tradingtips/{symbol}?count={count}");

        // Assert
        response.EnsureSuccessStatusCode();
        var tips = await response.Content.ReadFromJsonAsync<List<TradingTip>>();
        
        tips.Should().NotBeNull();
        tips.Should().HaveCount(1); // Based on SeedDatabase in StockTraderApiFactory for TESTMSFT
        tips.Should().ContainSingle(t => t.StockSymbol == symbol && t.Reason == "Test Buy Tip");
    }

    [Fact]
    public async Task GetRecentTips_SymbolWithNoTips_ShouldReturnNotFound() // Or empty list depending on API design
    {
        // Arrange
        string symbol = "TESTGOOG"; // This stock is seeded but has no tips in SeedDatabase
        int count = 5;
        // Current API design for GetRecentTips returns NotFound if !tips.Any()

        // Act
        var response = await _client.GetAsync($"/api/tradingtips/{symbol}?count={count}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task GetRecentTips_InvalidSymbol_ShouldReturnBadRequest()
    {
        // Arrange
        string symbol = " "; // Invalid symbol (empty or whitespace)
        int count = 5;

        // Act
        var response = await _client.GetAsync($"/api/tradingtips/{symbol}?count={count}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }


    [Fact]
    public async Task GetAllTips_ShouldReturnPaginatedSeededTips()
    {
        // Arrange
        int page = 1;
        int pageSize = 1; // Expecting 1 tip based on TESTAAPL seed on page 1 (after TESTMSFT)
                          // The order is by Timestamp descending. TESTMSFT is -1hr, TESTAAPL is -2hr.

        // Act
        var response = await _client.GetAsync($"/api/tradingtips?page={page}&pageSize={pageSize}");

        // Assert
        response.EnsureSuccessStatusCode();
        var tips = await response.Content.ReadFromJsonAsync<List<TradingTip>>();
        
        tips.Should().NotBeNull();
        tips.Should().HaveCount(1); 
        // The seed data has TESTMSFT tip at -1hr and TESTAAPL tip at -2hr.
        // Default order is descending by timestamp. So TESTMSFT tip should be first.
        tips.First().StockSymbol.Should().Be("TESTMSFT"); 


        // Act: Get second page
        page = 2;
        response = await _client.GetAsync($"/api/tradingtips?page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        tips = await response.Content.ReadFromJsonAsync<List<TradingTip>>();
        tips.Should().NotBeNull();
        tips.Should().HaveCount(1);
        tips.First().StockSymbol.Should().Be("TESTAAPL");
    }
    
    [Fact]
    public async Task GetAllTips_PageSizeTooLarge_ShouldReturnAllSeededTips()
    {
        // Arrange
        int page = 1;
        int pageSize = 10; // Larger than total seeded tips (2)

        // Act
        var response = await _client.GetAsync($"/api/tradingtips?page={page}&pageSize={pageSize}");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var tips = await response.Content.ReadFromJsonAsync<List<TradingTip>>();
        tips.Should().NotBeNull();
        tips.Should().HaveCount(2); // Total seeded tips
    }


    [Fact]
    public async Task GenerateTips_ValidSymbol_ShouldReturnOk()
    {
        // Arrange
        string symbol = "TESTMSFT"; // A stock that exists

        // Act
        var response = await _client.PostAsync($"/api/tradingtips/{symbol}/generate", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain($"Tip generation process initiated for {symbol.ToUpper()}");
        
        // As with historical data fetch, further testing here would involve checking
        // if new tips (based on mocked strategy engine) were actually created.
        // This can be done by calling the GET endpoint again after a short delay or
        // by having more control over the services in the TestServer.
        // For now, OK status is the primary check.
    }
    
    [Fact]
    public async Task GenerateTips_InvalidSymbol_ShouldReturnBadRequest()
    {
        // Arrange
        string symbol = " "; // Invalid symbol

        // Act
        var response = await _client.PostAsync($"/api/tradingtips/{symbol}/generate", null);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
}

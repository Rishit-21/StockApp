using Xunit;
using FluentAssertions;
using StockTrader.Core.Models; // For Stock, HistoricalPrice
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json; // For ReadFromJsonAsync, etc. (ensure .NET 5+ for this convenience)
using System.Threading.Tasks;
using System; // For DateTime
using System.Text.Json; // For JsonSerializer to handle potential issues if ReadFromJsonAsync has trouble
using System.Linq;


namespace StockTrader.Api.Tests;

// Using IClassFixture to share the factory instance across tests in the same class,
// which means they share the same in-memory database instance.
public class StocksControllerTests : IClassFixture<StockTraderApiFactory>
{
    private readonly HttpClient _client;
    private readonly StockTraderApiFactory _factory; // If direct access to factory needed for seeding/setup per test

    public StocksControllerTests(StockTraderApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(); // Creates an HttpClient that targets the in-memory server
    }

    [Fact]
    public async Task GetStocks_ShouldReturnSeededStocks()
    {
        // Act
        var response = await _client.GetAsync("/api/stocks");

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        var stocks = await response.Content.ReadFromJsonAsync<List<Stock>>();
        
        stocks.Should().NotBeNull();
        stocks.Should().HaveCount(3); // Based on SeedDatabase in StockTraderApiFactory
        stocks.Should().Contain(s => s.Symbol == "TESTMSFT");
        stocks.Should().Contain(s => s.Symbol == "TESTAAPL");
        stocks.Should().Contain(s => s.Symbol == "TESTGOOG");
    }

    [Fact]
    public async Task GetHistoricalData_ValidSymbolAndRange_ShouldReturnData()
    {
        // Arrange
        string symbol = "TESTMSFT"; // This stock is seeded with historical data
        var fromDate = DateTime.UtcNow.AddDays(-5).ToString("yyyy-MM-dd");
        var toDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await _client.GetAsync($"/api/stocks/{symbol}/historical?fromDate={fromDate}&toDate={toDate}");

        // Assert
        response.EnsureSuccessStatusCode();
        var prices = await response.Content.ReadFromJsonAsync<List<HistoricalPrice>>();
        prices.Should().NotBeNull();
        prices.Should().HaveCount(2); // Based on SeedDatabase
        prices.Should().OnlyContain(p => p.StockId == 101); // TESTMSFT's seeded ID
    }

    [Fact]
    public async Task GetHistoricalData_NonExistentSymbol_ShouldReturnNotFound()
    {
        // Arrange
        string symbol = "NOSUCHSTOCK";
        var fromDate = DateTime.UtcNow.AddDays(-5).ToString("yyyy-MM-dd");
        var toDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await _client.GetAsync($"/api/stocks/{symbol}/historical?fromDate={fromDate}&toDate={toDate}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task GetHistoricalData_InvalidDateRange_ShouldReturnBadRequest()
    {
        // Arrange
        string symbol = "TESTMSFT";
        var fromDate = DateTime.UtcNow.ToString("yyyy-MM-dd"); // fromDate AFTER toDate
        var toDate = DateTime.UtcNow.AddDays(-5).ToString("yyyy-MM-dd");

        // Act
        var response = await _client.GetAsync($"/api/stocks/{symbol}/historical?fromDate={fromDate}&toDate={toDate}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }


    [Fact]
    public async Task FetchHistoricalData_ValidSymbol_ShouldReturnOk()
    {
        // Arrange
        string symbol = "TESTMSFT"; // A stock that exists in the seeded data
        int years = 1;

        // Act
        var response = await _client.PostAsync($"/api/stocks/{symbol}/fetch-historical?years={years}", null); // No body content for this POST

        // Assert
        response.EnsureSuccessStatusCode(); // Should be 200 OK
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain($"Historical data fetch process initiated for {symbol.ToUpper()}");
        
        // Further verification could involve checking logs or, if the service wasn't mocked,
        // querying the historical data endpoint again to see if new (mocked) data was added.
        // For this test, confirming the endpoint is callable and returns success is the primary goal.
    }

    [Fact]
    public async Task FetchHistoricalData_InvalidSymbol_ShouldReturnBadRequest() // Or NotFound depending on API design, current is BadRequest
    {
        // Arrange
        string symbol = ""; // Invalid symbol
        int years = 1;

        // Act
        var response = await _client.PostAsync($"/api/stocks/{symbol}/fetch-historical?years={years}", null);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
}

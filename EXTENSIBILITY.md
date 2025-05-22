# Extensibility Guide for StockTrader Application

This document provides guidance on how to extend the StockTrader C# application, specifically focusing on adding new trading strategies and integrating new data sources.

## 1. Adding New Trading Strategies

Trading strategies are implemented within the `StockTrader.Service.Services.TradingStrategyEngine`.

**Location of Core Logic**: `StockTrader.Service/Services/TradingStrategyEngine.cs` (within the `GenerateTipsAsync` method).

### Steps to Add a New Strategy:

1.  **Identify Data Needs**: Determine what data your new strategy requires. This might include:
    *   Specific patterns in historical price/volume data.
    *   Correlations with live price movements.
    *   Specific sentiment score thresholds or patterns.
    *   Data from new sources (see section 2).
    *   Technical indicators (e.g., Moving Averages, RSI, MACD). If these are needed, they would typically be calculated from historical/live price data. You might consider creating helper methods or even dedicated technical indicator services.

2.  **Modify `TradingStrategyEngine.cs`**:
    *   Open `StockTrader.Service/Services/TradingStrategyEngine.cs`.
    *   Inside the `GenerateTipsAsync` method, after existing data fetching (live price, historical, sentiment), add your new strategy's logic.
    *   Your logic should analyze the available data and, if conditions are met, create a new `StockTrader.Core.Models.TradingTip` object.

3.  **Structure of a New Strategy Block (Conceptual Example)**:

    ```csharp
    // Inside GenerateTipsAsync method in TradingStrategyEngine.cs

    // --- New Custom Strategy: Example ---
    _logger.LogDebug($"Evaluating 'My Custom Strategy' for {stockSymbol}...");
    // 1. Fetch any additional specific data if not already available
    //    var customIndicator = await _technicalIndicatorService.CalculateEMA(stockSymbol, 20); // Example

    // 2. Implement your strategy's conditions
    bool condition1 = livePrice.Price > (decimal)150.0 && livePrice.Volume > 1000000;
    bool condition2 = averageSentiment > 0.25f;
    // bool condition3 = livePrice.Price > customIndicator.EMAValue; // Example with new data

    if (condition1 && condition2 /* && condition3 */)
    {
        tips.Add(new TradingTip
        {
            Timestamp = DateTime.UtcNow,
            StockSymbol = stockSymbol,
            TipType = TipType.Intraday, // Or Swing, Options as appropriate
            Action = ActionType.Buy,    // Or Sell, Hold
            Reason = "My Custom Strategy: Price above 150, high volume, positive sentiment.", // Be descriptive
            ConfidenceScore = 0.75f // Assign a confidence level
        });
        _logger.LogInformation($"'My Custom Strategy' triggered for {stockSymbol}.");
    }
    // --- End of New Custom Strategy ---
    ```

4.  **Testing**:
    *   Ensure your new strategy is evaluated correctly.
    *   Test with various data scenarios to confirm it generates tips as expected and doesn't cause unintended side effects.

### Advanced: Pluggable Strategies

For a more advanced and decoupled system, you could define an interface for strategies:

```csharp
// In StockTrader.Service.Interfaces
public interface ITradingStrategy
{
    string StrategyName { get; }
    Task<TradingTip?> EvaluateAsync(string stockSymbol, LivePrice currentPrice, IEnumerable<HistoricalPrice> historicalData, IEnumerable<SentimentData> sentimentData);
}
```

Then, `TradingStrategyEngine` could discover and run multiple strategy implementations:

1.  Create concrete strategy classes implementing `ITradingStrategy`.
2.  Register these strategies with DI (e.g., `services.AddScoped<ITradingStrategy, MyCustomStrategyImpl>();`).
3.  Inject `IEnumerable<ITradingStrategy>` into `TradingStrategyEngine`.
4.  Iterate through the strategies in `GenerateTipsAsync`, calling `EvaluateAsync` on each.

This approach makes adding new strategies cleaner as it doesn't require modifying `TradingStrategyEngine` each time.

## 2. Integrating New Data Sources

If your strategies require data not currently fetched by the application (e.g., macroeconomic indicators, specific financial statement data, alternative sentiment sources), you'll need to integrate new data sources.

### Steps to Integrate a New Data Source:

1.  **Define a Model (if necessary)**:
    *   If the new data has a distinct structure, create a new model class in `StockTrader.Core/Models/`.

2.  **Create a New Service Interface**:
    *   In `StockTrader.Service/Interfaces/`, define a new interface (e.g., `IMacroEconomicDataService`).
    *   Define methods for fetching the new data (e.g., `Task<IEnumerable<EconomicIndicator>> GetLeadingIndicatorsAsync();`).

3.  **Implement the Service**:
    *   In `StockTrader.Service/Services/`, create a new class implementing your interface (e.g., `MacroEconomicDataService.cs`).
    *   This service will be responsible for:
        *   Fetching data from the external API or source (see `API_INTEGRATIONS.md` for general guidance on API calls).
        *   Mapping the external data to your internal models.
        *   (Optional) Storing the data in the database if it needs to be persisted. This would involve:
            *   Adding a `DbSet` for the new model to `StockTraderDbContext.cs`.
            *   Using `IRepository<NewModel>` in your new service to save data.

4.  **Register the Service for DI**:
    *   In `StockTrader.Api/Program.cs` and `StockTrader.Worker/Program.cs`, register your new service:
        ```csharp
        // services.AddScoped<IMacroEconomicDataService, MacroEconomicDataService>();
        ```

5.  **Utilize in `TradingStrategyEngine`**:
    *   Inject your new service (e.g., `IMacroEconomicDataService`) into `TradingStrategyEngine.cs`.
    *   Call its methods in `GenerateTipsAsync` to fetch the data needed for your strategies.

### Example: Adding a Technical Indicator Calculation Service

If you need many technical indicators:

1.  **Models**: You might have models for indicator results if they are complex.
2.  **Interface**: `ITechnicalIndicatorService` with methods like `Task<decimal> CalculateSMAAsync(string symbol, int period)` or `Task<MACDOutput> CalculateMACDAsync(string symbol)`.
3.  **Implementation**: `TechnicalIndicatorService` would:
    *   Inject `IHistoricalDataService` to get the raw price/volume data.
    *   Perform calculations.
    *   Return the indicator values.
4.  **DI**: Register `ITechnicalIndicatorService`.
5.  **Usage**: Inject `ITechnicalIndicatorService` into `TradingStrategyEngine`.

## 3. Modifying Existing Mocked Services

As detailed in `API_INTEGRATIONS.md`:

*   **Historical Data**: Modify `HistoricalDataService.cs` to call a real financial API instead of `GenerateMockHistoricalData`.
*   **Live Data**: Modify `LiveMarketDataService.cs` to call a real live data API/WebSocket instead of `GenerateMockLivePrice`.
*   **Sentiment Analysis**: Modify `SentimentAnalysisService.cs` to call news/social media APIs and an NLP service instead of `GenerateMockSentimentData`.

Remember to handle API keys, rate limits, and error responses robustly when replacing mock implementations with live ones.
```

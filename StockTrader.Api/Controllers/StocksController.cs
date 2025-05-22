using Microsoft.AspNetCore.Mvc;
using StockTrader.Core.Models;
using StockTrader.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockTrader.Data.Repositories; // Ensured based on usage
using Microsoft.Extensions.Logging; // Ensured based on usage

namespace StockTrader.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StocksController : ControllerBase
{
    private readonly IHistoricalDataService _historicalDataService;
    private readonly IRepository<Stock> _stockRepository; // For listing stocks
    private readonly ILogger<StocksController> _logger;


    public StocksController(IHistoricalDataService historicalDataService, IRepository<Stock> stockRepository, ILogger<StocksController> logger)
    {
        _historicalDataService = historicalDataService;
        _stockRepository = stockRepository;
        _logger = logger;
    }

    // GET: api/stocks
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Stock>>> GetStocks()
    {
        try
        {
            var stocks = await _stockRepository.GetAllAsync();
            return Ok(stocks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all stocks.");
            return StatusCode(500, "Internal server error while retrieving stocks.");
        }
    }


    // GET: api/stocks/{symbol}/historical
    [HttpGet("{symbol}/historical")]
    public async Task<ActionResult<IEnumerable<HistoricalPrice>>> GetHistoricalData(
        string symbol,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return BadRequest("Stock symbol cannot be empty.");
        }

        DateTime actualFromDate = fromDate ?? DateTime.UtcNow.AddYears(-1); // Default to 1 year ago
        DateTime actualToDate = toDate ?? DateTime.UtcNow; // Default to today

        if (actualFromDate >= actualToDate)
        {
            return BadRequest("fromDate must be earlier than toDate.");
        }

        try
        {
            var data = await _historicalDataService.GetHistoricalDataAsync(symbol.ToUpper(), actualFromDate, actualToDate);
            if (!data.Any()) // data will not be null from service, just potentially empty
            {
                return NotFound($"No historical data found for {symbol.ToUpper()} in the given range.");
            }
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving historical data for {symbol.ToUpper()}.");
            return StatusCode(500, "Internal server error");
        }
    }

    // POST: api/stocks/{symbol}/fetch-historical?years=5
    // This is more of an admin/trigger endpoint
    [HttpPost("{symbol}/fetch-historical")]
    public async Task<IActionResult> FetchHistoricalData(string symbol, [FromQuery] int years = 10)
    {
         if (string.IsNullOrWhiteSpace(symbol))
        {
            return BadRequest("Stock symbol cannot be empty.");
        }
        if (years <= 0 || years > 20) // Max 20 years for safety
        {
            return BadRequest("Years must be between 1 and 20.");
        }
        try
        {
            await _historicalDataService.FetchAndStoreHistoricalDataAsync(symbol.ToUpper(), years);
            return Ok($"Historical data fetch process initiated for {symbol.ToUpper()} for the last {years} years.");
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, $"Error initiating historical data fetch for {symbol.ToUpper()}.");
            return StatusCode(500, "Internal server error during historical data fetch initiation.");
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using StockTrader.Core.Models;
using StockTrader.Service.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System; // For Exception
using Microsoft.Extensions.Logging; // Ensured based on usage

namespace StockTrader.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TradingTipsController : ControllerBase
{
    private readonly ITradingTipService _tipService;
    private readonly ILogger<TradingTipsController> _logger;

    public TradingTipsController(ITradingTipService tipService, ILogger<TradingTipsController> logger)
    {
        _tipService = tipService;
        _logger = logger;
    }

    // GET: api/tradingtips/{symbol}
    [HttpGet("{symbol}")]
    public async Task<ActionResult<IEnumerable<TradingTip>>> GetRecentTips(string symbol, [FromQuery] int count = 5)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return BadRequest("Stock symbol cannot be empty.");
        }
        if (count <=0 || count > 50)
        {
            return BadRequest("Count must be between 1 and 50.");
        }
        try
        {
            var tips = await _tipService.GetRecentTipsAsync(symbol.ToUpper(), count);
            if (!tips.Any()) // tips will not be null from service, just potentially empty
            {
                return NotFound($"No recent tips found for {symbol.ToUpper()}.");
            }
            return Ok(tips);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving tips for {symbol.ToUpper()}.");
            return StatusCode(500, "Internal server error");
        }
    }

    // GET: api/tradingtips
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TradingTip>>> GetAllTips([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page <=0) page = 1;
        if (pageSize <=0 || pageSize > 100) pageSize = 10;

        try
        {
            var tips = await _tipService.GetAllTipsAsync(page, pageSize);
            // No null check needed here as GetAllTipsAsync should return empty list if no tips, not null.
            return Ok(tips);
        }
        catch(Exception ex)
        {
             _logger.LogError(ex, "Error retrieving all tips.");
            return StatusCode(500, "Internal server error");
        }
    }

    // POST: api/tradingtips/{symbol}/generate
    // This is more of an admin/trigger endpoint
    [HttpPost("{symbol}/generate")]
    public async Task<IActionResult> GenerateTips(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return BadRequest("Stock symbol cannot be empty.");
        }
        try
        {
            // Note: ITradingStrategyEngine is not yet implemented, so this will fail
            // if called, until TradingStrategyEngine is created and registered.
            await _tipService.GenerateAndStoreTipsAsync(symbol.ToUpper());
            return Ok($"Tip generation process initiated for {symbol.ToUpper()}. Check logs and retrieve tips via GET endpoint.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error initiating tip generation for {symbol.ToUpper()}.");
            return StatusCode(500, "Internal server error during tip generation initiation.");
        }
    }
}

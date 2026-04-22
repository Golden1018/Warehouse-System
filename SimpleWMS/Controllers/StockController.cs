// ============================================================
// Controllers/StockController.cs
// ------------------------------------------------------------
// The Controller is the "front door" of the API.
// Its only job is to:
//   1. Receive an HTTP request.
//   2. Call the service to do the real work.
//   3. Return an HTTP response.
//
// Notice how thin this file is — no database code, no
// business rules. Everything is delegated to StockService.
// ============================================================

using Microsoft.AspNetCore.Mvc;
using SimpleWMS.Services;

namespace SimpleWMS.Controllers;

// [ApiController]   → Enables automatic model validation,
//                     automatic 400 responses for bad input, etc.
// [Route(...)]      → Base URL for all actions in this controller.
//                     [controller] is replaced by "Stock" at runtime.
[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    // Injected by ASP.NET Core — we declared it in Program.cs.
    // Using the interface (IStockService) rather than the
    // concrete class makes this easy to test and swap later.
    private readonly IStockService _stockService;

    public StockController(IStockService stockService)
    {
        _stockService = stockService;
    }

    // ── GET /api/stock ────────────────────────────────────────
    // Returns a list of all products with their current
    // quantities. Great for a "current inventory" dashboard.
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllStock()
    {
        var products = await _stockService.GetAllProductsAsync();
        return Ok(products); // HTTP 200 + JSON body
    }

    // ── GET /api/stock/logs ───────────────────────────────────
    // Returns all transaction log entries, newest first.
    // Each entry shows: product name, SKU, type (In/Out),
    // amount, and the date/time it happened.
    [HttpGet("logs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs()
    {
        var logs = await _stockService.GetAllLogsAsync();
        return Ok(logs);
    }

    // ── GET /api/stock/alerts ─────────────────────────────────
    // Returns only the products that are at or below their
    // ReorderPoint. The UI uses this to show the alert panel.
    [HttpGet("alerts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStockAlerts()
    {
        var lowStock = await _stockService.GetLowStockProductsAsync();
        return Ok(lowStock);
    }

    // ── POST /api/stock/in ────────────────────────────────────
    // Adds stock for a product (items arriving at warehouse).
    // The request body must contain SKU and Amount.
    //
    // Example request body:
    // {
    //   "sku": "WIDGET-001",
    //   "amount": 50
    // }
    [HttpPost("in")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StockIn([FromBody] StockRequest request)
    {
        var result = await _stockService.AddStockAsync(request.SKU, request.Amount);

        // If the service says it failed, return 400 Bad Request
        // with the error message explaining why.
        if (!result.Success)
            return BadRequest(new { error = result.Message });

        return Ok(new { message = result.Message });
    }

    // ── POST /api/stock/out ───────────────────────────────────
    // Removes stock for a product (items leaving the warehouse).
    // Same request body shape as /stock/in.
    //
    // Example request body:
    // {
    //   "sku": "WIDGET-001",
    //   "amount": 10
    // }
    [HttpPost("out")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StockOut([FromBody] StockRequest request)
    {
        var result = await _stockService.RemoveStockAsync(request.SKU, request.Amount);

        if (!result.Success)
            return BadRequest(new { error = result.Message });

        return Ok(new { message = result.Message });
    }
}

// ── Request DTO ───────────────────────────────────────────────
// DTO = Data Transfer Object — a simple class that holds the
// data coming in from the HTTP request body.
// Keeping it here (or in a Models folder) prevents the
// controller from being cluttered with property definitions.
public class StockRequest
{
    // The product's unique code (e.g. "WIDGET-001")
    public string SKU { get; set; } = string.Empty;

    // How many units to add or remove
    public int Amount { get; set; }
}

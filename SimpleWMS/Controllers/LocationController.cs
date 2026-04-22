// ============================================================
// Controllers/LocationController.cs
// ------------------------------------------------------------
// Handles all HTTP requests related to shelf locations.
//
// Endpoints:
//   GET  /api/locations           → list all shelves
//   POST /api/locations           → create a new shelf
//   PUT  /api/locations/assign    → move a product to a shelf
// ============================================================

using Microsoft.AspNetCore.Mvc;
using SimpleWMS.Services;

namespace SimpleWMS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationController : ControllerBase
{
    private readonly IStockService _stockService;

    public LocationController(IStockService stockService)
        => _stockService = stockService;

    // ── GET /api/locations ────────────────────────────────────
    // Returns every shelf and the products currently on it.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var locations = await _stockService.GetAllLocationsAsync();
        return Ok(locations);
    }

    // ── POST /api/locations ───────────────────────────────────
    // Creates a new shelf.
    // Request body: { "shelfCode": "A-01", "description": "Aisle A, Shelf 1" }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLocationRequest request)
    {
        var result = await _stockService.AddLocationAsync(
            request.ShelfCode, request.Description);

        if (!result.Success)
            return BadRequest(new { error = result.Message });

        return Ok(new { message = result.Message });
    }

    // ── PUT /api/locations/assign ─────────────────────────────
    // Moves a product to a shelf.
    // Request body: { "sku": "BOLT-M6", "locationId": 2 }
    [HttpPut("assign")]
    public async Task<IActionResult> Assign([FromBody] AssignLocationRequest request)
    {
        var result = await _stockService.AssignLocationAsync(
            request.SKU, request.LocationId);

        if (!result.Success)
            return BadRequest(new { error = result.Message });

        return Ok(new { message = result.Message });
    }
}

// ── Request DTOs ──────────────────────────────────────────────
public class CreateLocationRequest
{
    public string ShelfCode   { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class AssignLocationRequest
{
    public string SKU        { get; set; } = string.Empty;
    public int    LocationId { get; set; }
}

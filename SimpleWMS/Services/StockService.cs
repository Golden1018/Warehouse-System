// ============================================================
// Services/StockService.cs
// ------------------------------------------------------------
// The Service layer holds all the BUSINESS LOGIC.
// Controllers should be "thin" — they just receive HTTP
// requests and call the service. All the real work happens
// here.
//
// Why a separate service class?
//   • Easier to unit-test (no HTTP involved).
//   • Keeps the controller clean and readable.
//   • Business rules live in ONE place — easy to change.
// ============================================================

using Microsoft.EntityFrameworkCore;
using SimpleWMS.Data;
using SimpleWMS.Models;

namespace SimpleWMS.Services;

// ── Result wrapper ────────────────────────────────────────────
// Instead of throwing exceptions for expected errors
// (like "not enough stock"), we return a simple result object.
// This makes the controller code much cleaner.
public class ServiceResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; }

    private ServiceResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    // Factory helpers — e.g. ServiceResult.Ok("Done!")
    public static ServiceResult Ok(string message) => new(true, message);
    public static ServiceResult Fail(string message) => new(false, message);
}

// ── Interface ─────────────────────────────────────────────────
// Defining an interface lets ASP.NET Core's Dependency
// Injection wire things up, and makes testing easy later
// (you can swap in a "fake" service during tests).
public interface IStockService
{
    Task<List<Product>> GetAllProductsAsync();
    Task<List<Product>> GetLowStockProductsAsync();
    Task<List<StockLogDto>> GetAllLogsAsync();
    Task<ServiceResult> AddStockAsync(string sku, int amount);
    Task<ServiceResult> RemoveStockAsync(string sku, int amount);

    // ── Location methods ──────────────────────────────────
    Task<List<LocationDto>> GetAllLocationsAsync();
    Task<ServiceResult> AddLocationAsync(string shelfCode, string description);
    Task<ServiceResult> AssignLocationAsync(string sku, int locationId);
}

// ── DTOs for Location responses ───────────────────────────────
// We return a DTO (not the raw model) so we can include the
// list of products on each shelf in one neat object.

public class LocationDto
{
    public int    Id          { get; set; }
    public string ShelfCode   { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    // Summary of products on this shelf
    public List<ShelfProductDto> Products { get; set; } = new();
}

public class ShelfProductDto
{
    public string SKU      { get; set; } = string.Empty;
    public string Name     { get; set; } = string.Empty;
    public int    Quantity { get; set; }
}

// ── DTO for returning log entries ─────────────────────────────
// DTO = Data Transfer Object. We use this instead of returning
// the raw StockLog model so we can include the product name and
// SKU in one neat object — no need for the UI to do extra work.
public class StockLogDto
{
    public int      Id          { get; set; }
    public string   ProductName { get; set; } = string.Empty;
    public string   SKU         { get; set; } = string.Empty;
    public string   Type        { get; set; } = string.Empty;  // "In" or "Out"
    public int      Amount      { get; set; }
    public DateTime Date        { get; set; }
}

// ── Implementation ────────────────────────────────────────────
public class StockService : IStockService
{
    // We depend on the DbContext to talk to the database.
    // It is injected by ASP.NET Core — we never create it
    // manually with "new WarehouseDbContext()".
    private readonly WarehouseDbContext _db;

    public StockService(WarehouseDbContext db)
    {
        _db = db;
    }

    // ── GET ALL ───────────────────────────────────────────────
    // Returns every product with its current quantity.
    // AsNoTracking() tells EF not to track changes for these
    // objects — faster for read-only queries.
    public async Task<List<Product>> GetAllProductsAsync()
    {
        return await _db.Products
                        .AsNoTracking()
                        .Include(p => p.Location)  // join in the shelf info
                        .OrderBy(p => p.SKU)
                        .ToListAsync();
    }

    // ── GET LOW STOCK ─────────────────────────────────────────
    // Returns only products where the current Quantity is at
    // or below the ReorderPoint.
    // Example: Quantity=15, ReorderPoint=50 → included in list
    //          Quantity=80, ReorderPoint=50 → NOT included
    public async Task<List<Product>> GetLowStockProductsAsync()
    {
        return await _db.Products
                        .AsNoTracking()
                        .Include(p => p.Location)
                        .Where(p => p.Quantity <= p.ReorderPoint)
                        .OrderBy(p => p.Quantity)
                        .ToListAsync();
    }

    // ── GET ALL LOGS ──────────────────────────────────────────
    // Returns every StockLog entry joined with its Product so
    // we get the product name and SKU in the same result.
    // Include() is like SQL JOIN — it fetches the related Product
    // row at the same time as the StockLog row.
    // Newest entries come first (OrderByDescending on Date).
    public async Task<List<StockLogDto>> GetAllLogsAsync()
    {
        return await _db.StockLogs
                        .AsNoTracking()
                        .Include(l => l.Product)       // JOIN with Products table
                        .OrderByDescending(l => l.Date) // newest first
                        .Select(l => new StockLogDto
                        {
                            Id          = l.Id,
                            ProductName = l.Product.Name,
                            SKU         = l.Product.SKU,
                            Type        = l.Type,
                            Amount      = l.Amount,
                            Date        = l.Date
                        })
                        .ToListAsync();
    }

    // ── ADD STOCK (Inbound) ───────────────────────────────────
    // Steps:
    //   1. Find the product by SKU.
    //   2. Increase its Quantity.
    //   3. Write a StockLog entry with Type = "In".
    //   4. Save everything to the database in ONE transaction.
    public async Task<ServiceResult> AddStockAsync(string sku, int amount)
    {
        // Basic validation — amount must be positive
        if (amount <= 0)
            return ServiceResult.Fail("Amount must be greater than zero.");

        // Find the product — FirstOrDefaultAsync returns null
        // if nothing is found (no crash, just null).
        var product = await _db.Products
                               .FirstOrDefaultAsync(p => p.SKU == sku);

        if (product == null)
            return ServiceResult.Fail($"Product with SKU '{sku}' was not found.");

        // Update the quantity
        product.Quantity += amount;

        // Record the movement in the log
        var log = new StockLog
        {
            ProductId = product.Id,
            Type      = "In",
            Amount    = amount,
            Date      = DateTime.UtcNow
        };

        _db.StockLogs.Add(log);

        // SaveChangesAsync writes BOTH the product update AND
        // the new log in a single database transaction.
        await _db.SaveChangesAsync();

        return ServiceResult.Ok(
            $"Added {amount} units to '{product.Name}'. New quantity: {product.Quantity}.");
    }

    // ── REMOVE STOCK (Outbound) ───────────────────────────────
    // Same pattern as AddStock, but we also check there is
    // enough stock before removing. This prevents negative
    // inventory, which would be a data integrity problem.
    public async Task<ServiceResult> RemoveStockAsync(string sku, int amount)
    {
        if (amount <= 0)
            return ServiceResult.Fail("Amount must be greater than zero.");

        var product = await _db.Products
                               .FirstOrDefaultAsync(p => p.SKU == sku);

        if (product == null)
            return ServiceResult.Fail($"Product with SKU '{sku}' was not found.");

        // ── Business rule: cannot go below zero ───────────
        if (product.Quantity < amount)
            return ServiceResult.Fail(
                $"Not enough stock. Requested: {amount}, Available: {product.Quantity}.");

        product.Quantity -= amount;

        var log = new StockLog
        {
            ProductId = product.Id,
            Type      = "Out",
            Amount    = amount,
            Date      = DateTime.UtcNow
        };

        _db.StockLogs.Add(log);
        await _db.SaveChangesAsync();

        return ServiceResult.Ok(
            $"Removed {amount} units from '{product.Name}'. New quantity: {product.Quantity}.");
    }

    // ── GET ALL LOCATIONS ─────────────────────────────────────
    // Returns every shelf with the list of products on it.
    // Include() loads the Products navigation property so we
    // get both the shelf info and its contents in one query.
    public async Task<List<LocationDto>> GetAllLocationsAsync()
    {
        return await _db.Locations
                        .AsNoTracking()
                        .Include(l => l.Products)
                        .OrderBy(l => l.ShelfCode)
                        .Select(l => new LocationDto
                        {
                            Id          = l.Id,
                            ShelfCode   = l.ShelfCode,
                            Description = l.Description,
                            Products    = l.Products
                                           .Select(p => new ShelfProductDto
                                           {
                                               SKU      = p.SKU,
                                               Name     = p.Name,
                                               Quantity = p.Quantity
                                           })
                                           .ToList()
                        })
                        .ToListAsync();
    }

    // ── ADD LOCATION ──────────────────────────────────────────
    // Creates a new shelf. The shelf code must be unique.
    public async Task<ServiceResult> AddLocationAsync(string shelfCode, string description)
    {
        if (string.IsNullOrWhiteSpace(shelfCode))
            return ServiceResult.Fail("Shelf code cannot be empty.");

        // Check if this shelf code already exists
        var exists = await _db.Locations
                              .AnyAsync(l => l.ShelfCode == shelfCode.ToUpper());
        if (exists)
            return ServiceResult.Fail($"Shelf '{shelfCode.ToUpper()}' already exists.");

        _db.Locations.Add(new Location
        {
            ShelfCode   = shelfCode.ToUpper().Trim(),
            Description = description.Trim()
        });

        await _db.SaveChangesAsync();
        return ServiceResult.Ok($"Shelf '{shelfCode.ToUpper()}' created successfully.");
    }

    // ── ASSIGN LOCATION ───────────────────────────────────────
    // Moves a product to a different shelf.
    // This simply updates the LocationId on the product row.
    public async Task<ServiceResult> AssignLocationAsync(string sku, int locationId)
    {
        var product = await _db.Products
                               .FirstOrDefaultAsync(p => p.SKU == sku);
        if (product == null)
            return ServiceResult.Fail($"Product '{sku}' not found.");

        var location = await _db.Locations
                                .FirstOrDefaultAsync(l => l.Id == locationId);
        if (location == null)
            return ServiceResult.Fail("Shelf not found.");

        product.LocationId = locationId;
        await _db.SaveChangesAsync();

        return ServiceResult.Ok(
            $"'{product.Name}' moved to shelf {location.ShelfCode}.");
    }
}

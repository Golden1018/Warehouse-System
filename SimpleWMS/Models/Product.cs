// ============================================================
// Models/Product.cs
// ------------------------------------------------------------
// This class represents a physical product stored in the
// warehouse. Each row in the "Products" database table
// maps to one instance of this class.
//
// Entity Framework Core uses these properties to:
//   1. Create the database table (via migrations).
//   2. Read / write rows automatically.
// ============================================================

namespace SimpleWMS.Models;

public class Product
{
    // Primary key — EF automatically makes "Id" the PK and
    // sets it to auto-increment (AUTOINCREMENT in SQLite).
    public int Id { get; set; }

    // SKU = Stock Keeping Unit — a unique code per product
    // e.g. "WIDGET-001", "BOLT-M6"
    public string SKU { get; set; } = string.Empty;

    // Human-readable product name shown in reports / UI
    public string Name { get; set; } = string.Empty;

    // How many units are currently in the warehouse.
    // We update this every time stock comes in or goes out.
    public int Quantity { get; set; }

    // The minimum quantity we should always keep in stock.
    // If Quantity drops to or below this number, a Low Stock
    // Alert is triggered so staff know to reorder.
    // Example: ReorderPoint = 50 means "alert me when < 50 left"
    public int ReorderPoint { get; set; } = 50;

    // Foreign key — which shelf is this product stored on?
    // Nullable (int?) because a product may not be assigned to
    // a shelf yet — it shows as "Unassigned" in the UI.
    public int? LocationId { get; set; }

    // Navigation property back to the Location object.
    // The "?" means it can be null (no shelf assigned yet).
    public Location? Location { get; set; }

    // Navigation property — lets us load all stock log
    // entries that belong to this product in one query.
    // EF Core uses this to build the foreign-key relationship.
    public ICollection<StockLog> StockLogs { get; set; } = new List<StockLog>();
}

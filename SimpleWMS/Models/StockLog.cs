// ============================================================
// Models/StockLog.cs
// ------------------------------------------------------------
// Every time stock moves (in OR out) we write one row here.
// This gives us a full audit trail — we can always look back
// and see exactly what happened and when.
// ============================================================

namespace SimpleWMS.Models;

public class StockLog
{
    // Primary key — auto-increment
    public int Id { get; set; }

    // Foreign key — which product was moved?
    // EF Core sees "ProductId" and links it to Product.Id
    // automatically (convention-based naming).
    public int ProductId { get; set; }

    // Navigation property back to the Product.
    // The "null!" tells the compiler EF will always set this
    // before we use it, so we don't need to null-check it.
    public Product Product { get; set; } = null!;

    // "In"  = stock arrived at the warehouse
    // "Out" = stock left the warehouse
    public string Type { get; set; } = string.Empty;

    // How many units moved in this transaction
    public int Amount { get; set; }

    // When this movement happened — stored as UTC time
    public DateTime Date { get; set; } = DateTime.UtcNow;
}

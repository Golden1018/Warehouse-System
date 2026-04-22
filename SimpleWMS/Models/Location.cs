// ============================================================
// Models/Location.cs
// ------------------------------------------------------------
// A Location represents a physical shelf inside the warehouse.
// Example shelf codes: "A-01", "B-03", "C-12"
//
// One location can hold many products.
// One product belongs to one location (or none if unassigned).
// ============================================================

namespace SimpleWMS.Models;

public class Location
{
    // Primary key — auto-increment
    public int Id { get; set; }

    // Short code staff use to find the shelf physically.
    // e.g. "A-01" means Aisle A, Shelf 1.
    public string ShelfCode { get; set; } = string.Empty;

    // Optional longer description of the shelf location.
    // e.g. "Aisle A, Row 1 — Heavy items"
    public string Description { get; set; } = string.Empty;

    // Navigation property — all products currently on this shelf.
    // EF Core fills this automatically when you use .Include().
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

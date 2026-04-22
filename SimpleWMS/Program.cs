// ============================================================
// Program.cs
// ------------------------------------------------------------
// This is the entry point of the application.
// Everything is set up (wired together) here before the
// app starts listening for HTTP requests.
//
// Think of it as the "main switch panel" — you register
// all your services, configure middleware, and define the
// request pipeline.
// ============================================================

using Microsoft.EntityFrameworkCore;
using SimpleWMS.Data;
using SimpleWMS.Services;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Register Services with Dependency Injection ────────────
// "Services" are classes that the app needs to run.
// Registering them here means ASP.NET Core will create and
// inject them automatically wherever they are needed.

// Add controller support (finds all classes ending in "Controller").
// ReferenceHandler.IgnoreCycles fixes a crash that happens when two
// objects point to each other — e.g. Product → Location → Products →
// Product → ... JSON would loop forever without this setting.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

// Allow the browser UI to call the API from the same origin.
// CORS (Cross-Origin Resource Sharing) is required when the
// browser loads the page and then makes API calls.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Register our DbContext and tell it to use SQLite.
// The connection string comes from appsettings.json.
builder.Services.AddDbContext<WarehouseDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register our StockService.
// AddScoped means: create ONE instance per HTTP request,
// then discard it when the request is done.
// This is the right lifetime for database-related services.
builder.Services.AddScoped<IStockService, StockService>();

// Add Swagger — this auto-generates an interactive API
// documentation page where you can test your endpoints.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "Simple WMS API",
        Version     = "v1",
        Description = "A simple Warehouse Management System API for learning purposes."
    });
});

// ── 2. Build the app ──────────────────────────────────────────
var app = builder.Build();

// ── 3. Auto-create the database on startup ────────────────────
// EnsureCreated() checks if the database file exists.
// If it doesn't, it creates the tables based on our DbContext.
// NOTE: For production apps, use proper EF Migrations instead.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    db.Database.EnsureCreated(); // Creates warehouse.db + tables if missing

    // ── Seed shelf locations first ────────────────────────────
    if (!db.Locations.Any())
    {
        db.Locations.AddRange(
            new SimpleWMS.Models.Location { ShelfCode = "A-01", Description = "Aisle A, Shelf 1 — Small parts" },
            new SimpleWMS.Models.Location { ShelfCode = "A-02", Description = "Aisle A, Shelf 2 — Small parts" },
            new SimpleWMS.Models.Location { ShelfCode = "B-01", Description = "Aisle B, Shelf 1 — Cables & wiring" },
            new SimpleWMS.Models.Location { ShelfCode = "C-01", Description = "Aisle C, Shelf 1 — Protective gear" }
        );
        db.SaveChanges();
        Console.WriteLine("✓ Sample shelf locations seeded.");
    }

    // ── Seed sample products and assign them to shelves ───────
    if (!db.Products.Any())
    {
        // Fetch the location IDs we just created
        var locA1 = db.Locations.First(l => l.ShelfCode == "A-01").Id;
        var locA2 = db.Locations.First(l => l.ShelfCode == "A-02").Id;
        var locB1 = db.Locations.First(l => l.ShelfCode == "B-01").Id;
        var locC1 = db.Locations.First(l => l.ShelfCode == "C-01").Id;

        db.Products.AddRange(
            new SimpleWMS.Models.Product { SKU = "WIDGET-001", Name = "Blue Widget",  Quantity = 100, ReorderPoint = 50,  LocationId = locA1 },
            new SimpleWMS.Models.Product { SKU = "BOLT-M6",    Name = "M6 Bolt",      Quantity = 500, ReorderPoint = 100, LocationId = locA2 },
            new SimpleWMS.Models.Product { SKU = "CABLE-2M",   Name = "2m USB Cable", Quantity = 15,  ReorderPoint = 50,  LocationId = locB1 },
            new SimpleWMS.Models.Product { SKU = "GLOVE-LG",   Name = "Large Gloves", Quantity = 0,   ReorderPoint = 30,  LocationId = locC1 }
        );
        db.SaveChanges();
        Console.WriteLine("✓ Sample products seeded into the database.");
    }
}

// ── 4. Configure the HTTP request pipeline ────────────────────
// Middleware runs IN ORDER for every request.
// Think of it as a series of checkpoints each request passes.

// Swagger is always enabled (no Development check) so it works
// regardless of the environment setting on your machine.
app.UseSwagger();
// Swagger UI will be at: http://localhost:5000/swagger
app.UseSwaggerUI(c =>
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Simple WMS API v1"));

// NOTE: UseHttpsRedirection() is removed because we are only
// using HTTP (http://localhost:5000). Keeping it caused the
// "Failed to determine the https port" warning you saw.

// ── Serve the HTML UI from the wwwroot folder ─────────────────
// UseDefaultFiles makes "/" automatically serve "index.html".
// UseStaticFiles serves any file inside the wwwroot folder.
// ORDER MATTERS: DefaultFiles must come before StaticFiles.
app.UseDefaultFiles();
app.UseStaticFiles();

// Allow browser requests from any origin (needed for the UI)
app.UseCors();

app.MapControllers();      // Tell ASP.NET Core to route requests to our controllers

app.Run(); // Start the server — the app listens for requests here

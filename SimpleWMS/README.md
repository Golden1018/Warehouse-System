# SimpleWMS — Simple Warehouse Management System

A beginner-friendly Warehouse Management System built with **ASP.NET Core Web API (.NET 8)** and a plain **HTML/CSS/JavaScript** frontend. No complicated frameworks — just clean, well-commented code that is easy to read and learn from.

---

## What Does It Do?

SimpleWMS helps you track products stored in a warehouse. You can:

- See all products and how many units are in stock
- Add stock when new items arrive (Stock In)
- Remove stock when items leave (Stock Out)
- Get automatic alerts when a product is running low
- Assign products to specific shelf locations
- Move products between shelves
- View a full history of every stock movement

---

## Tech Stack

| Layer | Technology |
|---|---|
| **Backend** | ASP.NET Core Web API (.NET 8) |
| **Database** | SQLite via Entity Framework Core |
| **Frontend** | HTML + CSS + Vanilla JavaScript |
| **API Docs** | Swagger UI (built-in) |
| **Architecture** | Controller → Service → DbContext |

---

## Project Structure

```
SimpleWMS/
│
├── Models/                  # Database table definitions
│   ├── Product.cs           # Product: SKU, Name, Quantity, ReorderPoint, Location
│   ├── StockLog.cs          # Audit log: every stock movement recorded here
│   └── Location.cs          # Shelf location: ShelfCode, Description
│
├── Data/
│   └── WarehouseDbContext.cs # EF Core database context + Fluent API config
│
├── Services/
│   └── StockService.cs      # All business logic lives here
│
├── Controllers/
│   ├── StockController.cs   # Handles /api/stock endpoints
│   └── LocationController.cs# Handles /api/location endpoints
│
├── wwwroot/
│   └── index.html           # Single-page frontend UI
│
├── Program.cs               # App startup, DI registration, database seed
├── appsettings.json         # Connection string and port settings
├── SimpleWMS.csproj         # NuGet package list
└── warehouse.db             # SQLite database file (auto-created on first run)
```

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)


## How 2 run

### 1. Navigate to the project folder

```bash
cd "Warehouse system\SimpleWMS"
```

### 2. Restore NuGet packages

```bash
dotnet restore
```

### 3. Run the application

```bash
dotnet run
```

On the **first launch**, the app will automatically:
- Create the `warehouse.db` SQLite database file
- Create all tables (Products, StockLogs, Locations)
- Seed 4 shelf locations and 4 sample products

You will see output like:
```
✓ Sample shelf locations seeded.
✓ Sample products seeded into the database.
Now listening on: http://localhost:5000
Application started. Press Ctrl+C to shut down.
```

### 4. Open the UI

```
http://localhost:5000
```

### 5. Open Swagger (API testing page)

```
http://localhost:5000/swagger
```

### 6. Stop the server

```
Ctrl + C
```

---

## Features

### Inventory Dashboard

Displays all products in a table with:

| Column | Description |
|---|---|
| SKU | Unique product code (e.g. `BOLT-M6`) |
| Product Name | Full name of the product |
| Shelf | Which shelf the product is stored on |
| Quantity | Current units in stock |
| Reorder Point | Alert threshold — warns when stock falls to this level |
| Status | 🟢 In Stock / 🟡 Low Stock / 🔴 Out of Stock |

---

### Low Stock Alerts

A red alert panel appears automatically at the top of the page whenever any product's quantity is **at or below its Reorder Point**.

- 🔴 **Out of Stock** — quantity is 0
- 🟡 **Low Stock** — quantity is above 0 but at or below the Reorder Point

The panel disappears automatically once stock is replenished.

---

### Stock In / Stock Out

Two forms side by side let you quickly add or remove stock:

- Select a product from the dropdown (shows SKU, name, shelf, and current quantity)
- Enter the amount
- Click the button — the inventory table and alerts update instantly

**Rules enforced:**
- Amount must be greater than zero
- Stock Out will be rejected if there is not enough quantity available

---

### Shelf Location Management

Products are stored on named shelves inside the warehouse (e.g. `A-01`, `B-03`).

**Shelf overview cards** show:
- The shelf code and description
- Every product currently on that shelf and their quantities

**Add New Shelf** form lets you create new shelf locations with a code and optional description.

**Move Product to Shelf** lets you reassign any product to a different shelf. The inventory table updates to show the new shelf immediately.

---

### Transaction Log

A full audit trail of every stock movement, shown newest-first:

| Column | Description |
|---|---|
| # | Log entry ID |
| Date & Time | When the movement happened |
| SKU | Product code |
| Product Name | Full product name |
| Type | ▼ In (green) or ▲ Out (red) |
| Amount | Units moved (`+50` or `−10`) |

The log updates automatically after every Stock In or Stock Out action.

---

## API Endpoints

### Stock

| Method | URL | Description |
|---|---|---|
| `GET` | `/api/stock` | Get all products with current inventory |
| `GET` | `/api/stock/alerts` | Get only products at or below reorder point |
| `GET` | `/api/stock/logs` | Get all transaction logs (newest first) |
| `POST` | `/api/stock/in` | Add stock for a product |
| `POST` | `/api/stock/out` | Remove stock from a product |

**POST /api/stock/in and /api/stock/out — Request Body:**
```json
{
  "sku": "BOLT-M6",
  "amount": 50
}
```

---

### Locations

| Method | URL | Description |
|---|---|---|
| `GET` | `/api/location` | Get all shelves and their products |
| `POST` | `/api/location` | Create a new shelf |
| `PUT` | `/api/location/assign` | Move a product to a shelf |

**POST /api/location — Request Body:**
```json
{
  "shelfCode": "D-01",
  "description": "Aisle D, Shelf 1 — Overflow storage"
}
```

**PUT /api/location/assign — Request Body:**
```json
{
  "sku": "BOLT-M6",
  "locationId": 2
}
```

---

## Database Models

### Product

| Column | Type | Description |
|---|---|---|
| Id | int | Primary key, auto-increment |
| SKU | string | Unique product code |
| Name | string | Product name |
| Quantity | int | Current stock level |
| ReorderPoint | int | Minimum quantity before alert triggers |
| LocationId | int? | FK to Location (nullable — can be unassigned) |

### StockLog

| Column | Type | Description |
|---|---|---|
| Id | int | Primary key, auto-increment |
| ProductId | int | FK to Product |
| Type | string | `"In"` or `"Out"` |
| Amount | int | Units moved |
| Date | DateTime | UTC timestamp of the movement |

### Location

| Column | Type | Description |
|---|---|---|
| Id | int | Primary key, auto-increment |
| ShelfCode | string | Unique shelf code (e.g. `A-01`) |
| Description | string | Human-readable shelf description |

---

## Sample Data (seeded on first run)

### Shelf Locations

| ShelfCode | Description |
|---|---|
| A-01 | Aisle A, Shelf 1 — Small parts |
| A-02 | Aisle A, Shelf 2 — Small parts |
| B-01 | Aisle B, Shelf 1 — Cables & wiring |
| C-01 | Aisle C, Shelf 1 — Protective gear |

### Products

| SKU | Name | Qty | Reorder | Shelf | Status |
|---|---|---|---|---|---|
| WIDGET-001 | Blue Widget | 100 | 50 | A-01 | 🟢 In Stock |
| BOLT-M6 | M6 Bolt | 500 | 100 | A-02 | 🟢 In Stock |
| CABLE-2M | 2m USB Cable | 15 | 50 | B-01 | 🟡 Low Stock |
| GLOVE-LG | Large Gloves | 0 | 30 | C-01 | 🔴 Out of Stock |

---

## How to Reset the Database

If you want to start fresh (clear all data and re-seed):

1. Stop the server (`Ctrl + C`)
2. Delete the database file:
   ```bash
   del warehouse.db
   ```
3. Run the app again:
   ```bash
   dotnet run
   ```

The database will be recreated with the original sample data.

---

## Troubleshooting

| Problem | Fix |
|---|---|
| `dotnet` not recognized | Restart Command Prompt after installing .NET 8 SDK |
| Page shows "Cannot connect to API" | Make sure `dotnet run` is still running in Command Prompt |
| Swagger page not found | Use `http://` not `https://` |
| Port already in use | Change `"Urls"` in `appsettings.json` to another port (e.g. `5001`) |
| Dropdowns are empty | The API may have crashed — check the Command Prompt for error messages |
| Want to change a product's reorder point | Open `warehouse.db` with [DB Browser for SQLite](https://sqlitebrowser.org/) and edit directly |

---

## Key Concepts Used

| Concept | Where it is used |
|---|---|
| **Entity Framework Core** | `WarehouseDbContext.cs` — maps C# classes to database tables |
| **Fluent API** | `WarehouseDbContext.cs` — configures unique indexes and relationships |
| **Dependency Injection** | `Program.cs` — registers services so ASP.NET Core injects them automatically |
| **Service Layer** | `StockService.cs` — all business rules in one place, separate from HTTP logic |
| **DTO Pattern** | `StockLogDto`, `LocationDto` — shaped responses without circular references |
| **LINQ** | `StockService.cs` — queries the database using C# instead of raw SQL |
| **async / await** | Throughout — keeps the server responsive while waiting for the database |
| **Swagger** | Auto-generated API documentation at `/swagger` |
| **CORS** | `Program.cs` — allows the browser UI to call the API |
| **JSON Cycle Handling** | `Program.cs` — prevents crash when related objects reference each other |

---

## Quick Command Reference

```bash
# Install dependencies
dotnet restore

# Run the app
dotnet run

# Run on a specific port
dotnet run --urls "http://localhost:5050"

# Build without running (check for compile errors)
dotnet build

# Stop the server
Ctrl + C
```

---

*Built as a learning project for ASP.NET Core Web API with Clean Controller-Service-Repository architecture.*

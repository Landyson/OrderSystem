# OrderSystem (D1) – C# + ASP.NET Core (Minimal API) + MySQL
> Školní projekt PV – ukázka objednávkového systému splňující zadání D1 (Repository pattern) a požadavky na práci s RDBMS.

## Co to umí
- Evidence **zákazníků** a **produktů** (CRUD)
- Vytvoření **objednávky** v jednom kroku (uložení do více tabulek) – zákazník + objednávka + položky (+ volitelně platba)
- **Transakce** přes více tabulek: vytvoření objednávky + položek + odečet skladu
- **Reporty** (agregace) přes 3+ tabulek
- **Import** dat do 2 tabulek: Customers (CSV) + Products (JSON)
- **Konfigurace** v `appsettings.json`
- Webové UI (statické HTML) + REST API (Minimal API)

## Technologie
- .NET 8 (C#)
- ASP.NET Core Minimal API (bez MVC/Razor)
- MySQL Server (doporučeno 8.0+)
- NuGet balíček: `MySqlConnector` (driver; není ORM)

## Struktura repozitáře
- `/src/OrderSystem.Web` – aplikace (API + UI)
- `/sql` – vytvoření DB + view
- `/import` – ukázkové import soubory
- `/scripts` – pomocné skripty pro spuštění/publish

## 1) Příprava databáze (MySQL Workbench)
1. Spusť MySQL Server a přihlas se přes Workbench.
2. Vytvoř DB (např. `ordersystem`):
   ```sql
   CREATE DATABASE ordersystem CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
   ```
3. Otevři a spusť skripty:
   - `sql/01_schema.sql`
   - `sql/02_views.sql`

> Pokud nechceš vytvářet uživatele, můžeš použít root účet. V testovacích scénářích ale nedoporučuju dávat root heslo do repa – použij `appsettings.example.json`.

## 2) Konfigurace aplikace
V souboru:
- `src/OrderSystem.Web/appsettings.json`

Uprav:
- `ConnectionStrings:MySql`
- `App:BaseUrl` (volitelné)

Příklad connection stringu:
```
server=localhost;port=3306;database=ordersystem;user=root;password=YOUR_PASSWORD;TreatTinyAsBoolean=true;
```

## 3) Spuštění (bez IDE)
### Varianta A – přímo dotnet run (nejjednodušší)
Na Windows v rootu projektu:
```bat
scripts\run.bat
```
Pak otevři:
- UI: `http://localhost:5000`
- API Swagger: `http://localhost:5000/swagger`

### Varianta B – publish (kdyby na školním PC nebyl nainstalovaný .NET)
```bat
scripts\publish_win_x64_selfcontained.bat
```
Výstup bude v:
- `src\OrderSystem.Web\bin\Release\net8.0\win-x64\publish\`
Spuštění:
- `OrderSystem.Web.exe`

## 4) Import ukázkových dat
- Customers (CSV): `import/customers.csv`
- Products (JSON): `import/products.json`
V UI najdeš stránku **Import**.

## 5) Základní API endpointy
- `GET /api/customers`
- `POST /api/customers`
- `GET /api/products`
- `POST /api/products`
- `POST /api/orders` (transakční vytvoření objednávky)
- `GET /api/reports/top-customers`
- `GET /api/reports/product-sales`
- `POST /api/import/customers` (CSV multipart)
- `POST /api/import/products` (JSON multipart)


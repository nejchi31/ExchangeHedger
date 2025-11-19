# Hedger – Exchange Order Plan Executor

Hedger is a small .NET solution that simulates a **exchange** order plan on top of multiple order books on n exchanges.

Given:

- N order books (treated as N different exchanges),
- an order type (**Buy** or **Sell**),
- and a BTC amount,

the system:

- Finds the **best execution plan** across all exchanges  
  - **Buy** → minimize total EUR spent (use **cheapest asks** first)  
  - **Sell** → maximize total EUR received (use **highest bids** first)
- Respects **per-exchange balances** (EUR and BTC)
- Outputs the exact set of orders that a “Hedger” component would send to underlying exchanges

The functionality is exposed via:

- A **Console application** (Part 1 of the task)
- A **Web API** with Swagger UI (Part 2)
- A **test project** with unit & integration tests (Bonus)

## Tech Stack

- **.NET** 9 (SDK / Runtime)
- **ASP.NET Core** Web API (Kestrel)
- **xUnit** for tests
- **Newtonsoft.Json** for parsing order book JSON
- **Docker** for containerizing API and console apps

- ## Solution Structure

```text
MetaExchange.sln
│
├─ Hedger.Core        # Domain models, services, repositories
├─ Hedger.Console     # Console app (Part 1)
├─ Hedger.Api         # Web API with Swagger (Part 2)
└─ Hedger.Tests       # xUnit tests (Bonus)
```


### Core domain

Key types (in `Hedger.Core`):

- `OrderTypeEnum` – `Buy` or `Sell`
- `OrderBookEntry` – `{ Price, Amount }`
- `OrderBook` – `{ Bids: List<OrderBookEntry>, Asks: List<OrderBookEntry> }`
- `ExchangeState` – `{ Exchange, EurBalance, BtcBalance, OrderBook }`
- `ExecutionOrder` – a single order to execute on one exchange
- `ExchangeResult`
  - `FullyFilled`
  - `TotalBtc`
  - `TotalEur`
  - `Orders: List<ExecutionOrder>`
- `ExchangeConfigModel`
  - `ExchangeGenerateCount`
  - `EurBalanceMin` / `EurBalanceMax`
  - `BtcBalanceMin` / `BtcBalanceMax`

### Services & Repositories

- `IExchangeService` / `ExchangeService`
  - `ExchangeResult ExecuteOrder(List<ExchangeState> exchanges, OrderTypeEnum orderType, decimal targetBtc)`

- `IExchangeRepository` / `ExchangeRepository`
  - `List<ExchangeState> GetExchanges(ExchangeConfigModel config)`

---

## Order Book Data

The project uses a single large file (e.g. `order_books_data`) with many snapshots:

```text
<timestamp>\t{ JSON }
<timestamp>\t{ JSON }
...
```

Each JSON object contains:

```json
{
  "AcqTime": "2019-01-29T11:00:00Z",
  "Bids": [ { "Order": { "Amount": 1.0, "Price": 29500.0 } }, ... ],
  "Asks": [ { "Order": { "Amount": 0.5, "Price": 30000.0 } }, ... ]
}
```

The repository:

1. Reads all valid lines.
2. Randomly selects `ExchangeGenerateCount` lines.
3. Treats each selected line as **one exchange** with:
   - Its own `OrderBook` (bids/asks)
   - Random EUR/BTC balances within the configured min/max ranges

> Make sure the data file is added to the appropriate project (`Hedger.Api/order_books_data`, `Hedger.Console/order_books_data`) and marked as:
> - **Build Action** = `Content`
> - **Copy to Output Directory** = `Copy if newer`

---

## Matching Algorithm (MetaExchangeService)

### Buy flow

1. Collect all **asks** from all exchanges into one flat list.
2. Sort by **price ascending** (cheapest first).
3. Iterate levels:
   - For each ask, compute max BTC that can be bought:
     - limited by:
       - order’s available amount,
       - exchange’s **EUR balance**,
       - remaining BTC needed.
4. Deduct EUR from that exchange, add `ExecutionOrder` (`Buy`).
5. Stop when:
   - Target BTC is bought → `FullyFilled = true`, or
   - No more affordable liquidity / balances → `FullyFilled = false`.

### Sell flow

1. Collect all **bids** from all exchanges into one flat list.
2. Sort by **price descending** (highest first).
3. Iterate levels:
   - For each bid, compute max BTC that can be sold:
     - limited by:
       - bid’s available amount,
       - exchange’s **BTC balance**,
       - remaining BTC to sell.
4. Deduct BTC from that exchange, add `ExecutionOrder` (`Sell`).
5. Stop when:
   - Target BTC is sold → `FullyFilled = true`, or
   - No more bids / balances → `FullyFilled = false`.

Result:

- Always sells at **highest prices first**.
- Always buys at **lowest prices first**.
- Never exceeds per-exchange EUR/BTC balances.

---

## Running the Console App (Part 1)

### From Visual Studio

1. Set **Hedger.Console** as the **Startup Project**.
2. Ensure `order_books_data` is in `Hedger.Console/` and set to `Content + Copy if newer`.
3. Press **F5** (or `Ctrl+F5`).

### From CLI

From the solution root:

```bash
dotnet run --project Hedger.Console/Hedger.Console.csproj
```

### Console flow

The console prompts:

1. `How many exchanges to generate?` (order_books_data consists of N order book from different exchanges, therefore we reduce number of exchanges for testing purposes)
2. `EUR balance min / max`
3. `BTC balance min / max`
4. `Order type (Buy/Sell)`
5. `Amount BTC`

Then it:

- Shows **top N bids/asks per exchange** (depending on buy/sell).
- Computes and prints the **best execution plan**, e.g.:

```text
=== Best Execution Plan ===
Fully filled: True
Total BTC:    7.00000
Total EUR:    20706.7602185

Orders to execute:
  Ex1: Sell 0.00185 BTC @ 2963.01 EUR
  Ex1: Sell 2.99815 BTC @ 2963.00 EUR
  Ex2: Sell 0.01 BTC @ 2954.46 EUR
  Ex2: Sell 3.99 BTC @ 2954.44 EUR
```

---

## Running the Web API (Part 2)

### Local (without Docker)

From solution root:

```bash
dotnet run --project Hedger.Api/Hedger.Api.csproj
```

By default the API listens on the URLs from `launchSettings.json`, e.g.:

- `http://localhost:5156`
- `https://localhost:7256`

Swagger UI should be available at:

- `http://localhost:5156/swagger`

### API endpoint

`POST /api/exchange/execute-order-plan`

**Request body example:**
```json
{
  "orderType": "Buy",
  "amountBtc": 2.5
}
```

The controller:

1. Validates the request (order type, amount).
2. Builds an `ExchangeConfigModel` (e.g. 3 exchanges, EUR 0–40,000, BTC 0–5).
3. Loads exchanges via `IExchangeRepository`.
4. Calls `IExchangeService.ExecuteOrder(...)`.
5. Returns a `ExecutionResponseDTO`:

```json
{
  "fullyFilled": true,
  "totalBtc": 2.5,
  "totalEur": 74500.0,
  "orders": [
    {
      "exchangeId": "Ex1",
      "orderType": "Buy",
      "amountBtc": 1.2,
      "price": 29750.0
    },
    {
      "exchangeId": "Ex3",
      "orderType": "Buy",
      "amountBtc": 1.3,
      "price": 30200.0
    }
  ]
}
```

### Global exception middleware

The API has a custom `ExceptionMiddleware` that:

- Logs unhandled exceptions
- Maps:
  - `ArgumentException` → `400 Bad Request`
  - `FileNotFoundException` → `404 Not Found`
  - Default → `500 Internal Server Error`
- Returns a JSON error payload with:
  - `type`, `title`, `status`, `code`, `detail` (in Development), `traceId`

Example error:

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "An error occurred while processing your request.",
  "status": 400,
  "code": "bad_request",
  "detail": "AmountBtc must be positive.",
  "traceId": "0HMQ7..."
}
```

---

## Docker

### API Dockerfile

Located in `Hedger.Api/Dockerfile` (multi-stage build):

- Build stage:
  - `mcr.microsoft.com/dotnet/sdk:9.0`
  - Restore & publish `Hedger.Api`
- Runtime stage:
  - `mcr.microsoft.com/dotnet/aspnet:9.0`
  - App runs on port `8080` inside container

Build & run from solution root:

```bash
# build
docker build -t hedger-api -f Hedger.Api/Dockerfile .

# run (Development environment to enable Swagger)
docker run --name hedger-api -d -p 8080:8080 hedger-api
```

Swagger: `http://localhost:8080/swagger`.

### Console Dockerfile

Located in `Hedger.Console/Dockerfile`:

- Build stage:
  - `mcr.microsoft.com/dotnet/sdk:9.0`
  - Restore & publish `Hedger.Console`
- Runtime stage:
  - `mcr.microsoft.com/dotnet/runtime:9.0`

Build & run from root solution:

```bash
docker build -t hedger-console -f Hedger.Console/Dockerfile .

# interactive run
docker run -it hedger-console --name hedger-console
```

You’ll see the console prompts in your terminal.

---

## Tests (Bonus Task)

Project: `Hedger.Tests` (xUnit).

1. **Service unit tests** (`MetaExchangeService`):
   - Buy:
     - Fully filled, uses **cheapest asks** across multiple exchanges.
   - Sell:
     - Fully filled, uses **highest bids** across multiple exchanges.
   - Balance constraints:
     - EUR balance limits purchases (partial fills).
     - BTC balance limits sells (partial fills).
   - No liquidity:
     - No asks when buying → no orders, `FullyFilled = false`.
     - No bids when selling → no orders, `FullyFilled = false`.
   - Invalid input:
     - Non-positive BTC amount → throws `ArgumentOutOfRangeException`.

2. **Integration test** (repository + service):
   - Creates a temporary mini `order_books_data` file with two lines.
   - Uses `ExchangeRepository.GetExchanges(config)` to load 2 exchanges.
   - Runs `ExecuteMetaOrder` (e.g. buy 1.5 BTC).
   - Asserts that:
     - Result is fully filled.
     - Orders are placed on the **best priced exchanges** according to the snapshots.

### Running tests

From solution root:

```bash
dotnet test
```

---


# Aspire Challenge 3 — Pizza Delivery Microservices with Dapr & .NET Aspire

This solution demonstrates a **pizza ordering and delivery system** built as a set of microservices orchestrated with [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) and [Dapr](https://dapr.io/) (Distributed Application Runtime). It is the completed "after" state for **Challenge 3** of the Dapr + Aspire workshop.

## Solution Overview

The application models a full pizza order lifecycle — from storefront intake through cooking and delivery — using four independent ASP.NET Core Web API services that communicate via Dapr building blocks.

```
┌──────────────┐   Service Invocation   ┌──────────────┐   Service Invocation   ┌──────────────┐
│   Pizza      │ ────────────────────►  │   Pizza      │ ────────────────────►  │   Pizza      │
│  Storefront  │                        │   Kitchen    │                        │  Delivery    │
│  (3502)      │                        │   (3503)     │                        │  (3504)      │
└──────┬───────┘                        └──────┬───────┘                        └──────┬───────┘
       │ Pub/Sub                               │ Pub/Sub                               │ Pub/Sub
       ▼                                       ▼                                       ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────────┐
│                              pizzapubsub  (Redis Pub/Sub)                                      │
│                                    Topic: "orders"                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────┘
       │                                                                                          
       ▼                                                                                          
┌──────────────┐                                                                                  
│   Pizza      │◄──── Subscribes to "orders" topic                                               
│   Order      │                                                                                  
│  (3501)      │──── State Store (Redis) ──── pizzastatestore                                    
└──────────────┘                                                                                  
```

## Projects

| Project | Dapr App ID | Dapr HTTP Port | Description |
|---|---|---|---|
| **DaprWorkshop.AppHost** | — | — | .NET Aspire orchestrator that wires up all services and Dapr sidecars |
| **DaprWorkshop.ServiceDefaults** | — | — | Shared Aspire service defaults (OpenTelemetry, health checks, service discovery, resilience) |
| **PizzaStorefront** | `pizza-storefront` | 3502 | Entry point for new orders; orchestrates the full order flow |
| **PizzaKitchen** | `pizza-kitchen` | 3503 | Simulates pizza cooking through multiple stages |
| **PizzaDelivery** | `pizza-delivery` | 3504 | Simulates pizza delivery through multiple stages |
| **PizzaOrder** | `pizzaorder` | 3501 | Manages order state; subscribes to order status updates via pub/sub |

## Dapr Building Blocks Used

### 1. Publish / Subscribe (`pizzapubsub`)

- **Component**: `pubsub.redis` backed by a local Redis instance on `localhost:6379`
- **Topic**: `orders`
- All four services publish order status updates to the `orders` topic as the order progresses through its lifecycle.
- **PizzaOrder** subscribes to the `orders` topic (`[Topic("pizzapubsub", "orders")]`) and persists every status change to the state store.

### 2. State Management (`pizzastatestore`)

- **Component**: `state.redis` backed by the same local Redis instance.
- **PizzaOrder** uses the Dapr state store to save, retrieve, and delete order state (keyed as `order_{orderId}`).
- Supports actor state store (`actorStateStore: true`).

### 3. Service Invocation

- **PizzaStorefront** uses `DaprClient.InvokeMethodAsync` to call:
  - `pizza-kitchen` → `POST /cook` (cook the pizza)
  - `pizza-delivery` → `POST /delivery` (deliver the pizza)
- Dapr handles service discovery, mTLS, retries, and observability automatically.

## Order Lifecycle

When a new order is submitted to the **Storefront**, it flows through these stages:

| # | Status | Service | Description |
|---|---|---|---|
| 1 | `validating` | Storefront | Order validation begins |
| 2 | `processing` | Storefront | Order is being processed |
| 3 | `confirmed` | Storefront | Order confirmed and ready for kitchen |
| 4 | `cooking_preparing_ingredients` | Kitchen | Ingredients are being prepared |
| 5 | `cooking_making_dough` | Kitchen | Dough is being made |
| 6 | `cooking_adding_toppings` | Kitchen | Toppings are being added |
| 7 | `cooking_baking` | Kitchen | Pizza is in the oven |
| 8 | `cooking_quality_check` | Kitchen | Final quality check |
| 9 | `cooked` | Kitchen | Pizza is ready |
| 10 | `delivery_finding_driver` | Delivery | Looking for a delivery driver |
| 11 | `delivery_driver_assigned` | Delivery | Driver has been assigned |
| 12 | `delivery_picked_up` | Delivery | Pizza picked up by driver |
| 13 | `delivery_on_the_way` | Delivery | Driver is en route |
| 14 | `delivery_arriving` | Delivery | Driver is arriving |
| 15 | `delivery_at_location` | Delivery | Driver is at the delivery location |
| 16 | `delivered` | Delivery | Order delivered successfully |

Each status change is published to the `orders` pub/sub topic and persisted by the **PizzaOrder** service.

## .NET Aspire Integration

The **AppHost** project uses .NET Aspire to orchestrate all services:

- Each service is registered with `builder.AddProject<T>()` and configured with a Dapr sidecar via `.WithDaprSidecar()`.
- Dapr component YAML files are loaded from a shared `aspire-shared/resources` directory.
- The **Dapr Dashboard** is added as an executable resource on port `8080`.
- **ServiceDefaults** provides:
  - **OpenTelemetry** — distributed tracing, metrics, and structured logging exported via OTLP.
  - **Health checks** — `/health` (readiness) and `/alive` (liveness) endpoints.
  - **Service discovery** and **HTTP resilience** for all `HttpClient` instances.

## Tech Stack

| Technology | Version / Details |
|---|---|
| .NET | 10.0 |
| .NET Aspire AppHost SDK | 13.1.1 |
| Dapr | Via `CommunityToolkit.Aspire.Hosting.Dapr` + `Dapr.AspNetCore` |
| Redis | Local instance for pub/sub and state store |
| OpenAPI / Scalar | API documentation in development mode |
| OpenTelemetry | Tracing, metrics, and logging |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Dapr CLI](https://docs.dapr.io/getting-started/install-dapr-cli/) with `dapr init` completed
- [Docker](https://www.docker.com/) (for Redis, started automatically by `dapr init`)
- .NET Aspire workload (`dotnet workload install aspire`)

## Running the Solution

1. **Start the Aspire AppHost:**

   ```bash
   cd DaprWorkshop.AppHost
   dotnet run
   ```

2. **Open the Aspire Dashboard** — the URL is printed in the console output (typically `https://localhost:17241`).

3. **Open the Dapr Dashboard** at `http://localhost:8080`.

4. **Place an order** using the Storefront endpoint:

   ```http
   POST http://localhost:<storefront-port>/storefront/order
   Content-Type: application/json

   {
       "orderId": "123",
       "pizzaType": "pepperoni",
       "size": "large",
       "customer": {
           "name": "John Doe",
           "address": "123 Main St",
           "phone": "555-0123"
       }
   }
   ```

5. **Check order status** via the Order service:

   ```http
   GET http://localhost:<order-port>/order/123
   ```

## API Endpoints

### PizzaStorefront

| Method | Path | Description |
|---|---|---|
| `POST` | `/storefront/order` | Submit a new pizza order |

### PizzaOrder

| Method | Path | Description |
|---|---|---|
| `POST` | `/order` | Create an order directly |
| `GET` | `/order/{orderId}` | Get order by ID |
| `DELETE` | `/order/{orderId}` | Delete an order |
| `POST` | `/orders-sub` | *(Dapr subscription handler — not called directly)* |

### PizzaKitchen

| Method | Path | Description |
|---|---|---|
| `POST` | `/cook` | Cook a pizza (called via Dapr service invocation) |

### PizzaDelivery

| Method | Path | Description |
|---|---|---|
| `POST` | `/delivery` | Deliver a pizza (called via Dapr service invocation) |

## Dapr Resource Configuration

Component YAML files are located in `../../aspire-shared/resources/` (relative to the AppHost):

- **`pubsub.yaml`** — Redis-backed pub/sub component (`pizzapubsub`)
- **`statestore.yaml`** — Redis-backed state store component (`pizzastatestore`)

## Project Structure

```
07-aspire-challenge-3/
├── DaprWorkshop.slnx                    # Solution file
├── DaprWorkshop.AppHost/                # .NET Aspire orchestrator
│   └── Program.cs                       # Service & Dapr sidecar registration
├── DaprWorkshop.ServiceDefaults/        # Shared service configuration
│   └── Extensions.cs                    # OpenTelemetry, health checks, resilience
├── PizzaStorefront/                     # Order intake & orchestration service
│   ├── Controllers/StorefrontController.cs
│   ├── Services/StorefrontService.cs    # Orchestrates order → cook → deliver
│   └── Models/OrderModels.cs
├── PizzaKitchen/                        # Cooking simulation service
│   ├── Controllers/CookController.cs
│   ├── Services/CookService.cs          # Multi-stage cooking simulation
│   └── Models/OrderModels.cs
├── PizzaDelivery/                       # Delivery simulation service
│   ├── Controllers/DeliveryController.cs
│   ├── Services/DeliveryService.cs      # Multi-stage delivery simulation
│   └── Models/OrderModels.cs
└── PizzaOrder/                          # Order state management service
    ├── Controllers/OrderController.cs   # CRUD + pub/sub subscription
    ├── Services/OrderStateService.cs    # Dapr state store operations
    └── Models/OrderModels.cs
```

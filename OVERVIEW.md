# Overview: Dapr + Aspire Microservices Workshop

## Architecture

A progressive workshop teaching microservices with **Dapr** (Distributed Application Runtime) and **.NET Aspire**. The domain is a **pizza ordering system** with services for ordering, cooking, delivery, and state tracking. All projects target **.NET 10.0** and use **Central Package Management** via a shared `Directory.Packages.props`.

### Key Packages (Central)

- `Dapr.AspNetCore` 1.17.0
- `Dapr.Workflow` 1.17.0
- `CommunityToolkit.Aspire.Hosting.Dapr` 13.0.0
- `Aspire.AppHost.Sdk` 13.1.2
- `OpenTelemetry.*` 1.15.0
- `Scalar.AspNetCore` 2.12.50

### Microservices

| Service | Dapr App ID | Port | Dapr HTTP Port | Role |
|---|---|---|---|---|
| **PizzaOrder** | `pizza-order` | 8001 | 3501 | Order state management, subscribes to pub/sub |
| **PizzaStorefront** | `pizza-storefront` | 8002 | 3502 | Entry point for orders, orchestrates flow |
| **PizzaKitchen** | `pizza-kitchen` | 8003 | 3503 | Multi-stage cooking simulation |
| **PizzaDelivery** | `pizza-delivery` | 8004 | 3504 | Multi-stage delivery simulation |
| **PizzaWorkflow** | `pizza-workflow` | 8005 | 3505 | Dapr Workflow orchestrator (challenge 4+) |

---

## Folder Structure

```
after/
├── 01-dapr-challenge-1/        # Dapr State Store
├── 02-dapr-challenge-2/        # Dapr Service Invocation
├── 03a-dapr-challenge-3/       # Dapr Pub/Sub (declarative subscription)
├── 03b-dapr-challenge-3/       # Dapr Pub/Sub (programmatic subscription)
├── 04-dapr-challenge-4/        # Dapr Workflow
├── 05-aspire-challenge-1/      # .NET Aspire Basics
├── 06-aspire-challenge-2/      # Aspire + Service Invocation
├── 07-aspire-challenge-3/      # Aspire + Pub/Sub
├── 08-aspire-challenge-4/      # Aspire + Dapr Workflow
├── 09-aspire-challenge-5/      # Aspire + Event-Driven Workflow
├── shared/                     # Dapr component files (challenges 01-04)
├── aspire-shared/              # Dapr component files (challenges 05-08)
└── Update-AspirePackages.ps1   # Package update script
```

---

## Per-Challenge Overview

### 01 - Dapr State Store

**Purpose:** Introduces Dapr **State Management** with a single service.

**Services:** PizzaOrder

**Technologies:**
- Dapr State Management (`state.redis` — component name `pizzastatestore`)
- Redis (localhost:6379)

**Key Code:**
- `OrderController` — CRUD for orders + `/orders-sub` endpoint accepting CloudEvent-wrapped Orders
- `OrderStateService` — Uses `DaprClient` for `GetStateAsync`, `SaveStateAsync`, `DeleteStateAsync`
- Order model: `OrderId`, `PizzaType`, `Size`, `Customer`, `Status`, `Error`

---

### 02 - Dapr Service Invocation

**Purpose:** Introduces Dapr **Service Invocation** with multiple cooperating services.

**Services:** PizzaStorefront, PizzaKitchen, PizzaDelivery, PizzaOrder (4 services)

**Technologies:**
- Dapr Service Invocation (HttpClient via AddKeyedSingleton and DaprClient.CreateInvokeHttpClient)
- Dapr State Management (from challenge 1)

**Communication Pattern:** Synchronous request/response via Dapr service invocation: Storefront → Kitchen → Delivery (chain)

**Key Code:**
- `StorefrontService` — Orchestrates flow: validates order → calls `pizza-kitchen`/`cook` → calls `pizza-delivery`/`delivery`
- `CookService` — Simulates 5 cooking stages (preparing_ingredients → making_dough → adding_toppings → baking → quality_check)
- `DeliveryService` — Simulates 6 delivery stages (finding_driver → assigned → picked_up → on_the_way → arriving → at_location)

---

### 03a - Dapr Pub/Sub (Declarative Subscription)

**Purpose:** Introduces Dapr **Publish/Subscribe** with **declarative subscriptions** (YAML-based).

**Services:** All 4 core services

**Technologies:**
- Dapr Pub/Sub (`pubsub.redis` — component name `pizzapubsub`, topic `orders`)
- Declarative subscription via YAML (`subscription.yaml`)
- Dapr Service Invocation (retained from challenge 2)

**Key Changes from Challenge 2:**
- `StorefrontService`, `CookService`, `DeliveryService` publish status updates to `pizzapubsub`/`orders` via `_daprClient.PublishEventAsync`
- `PizzaOrder` adds `app.UseCloudEvents()` for CloudEvent deserialization
- Storefront still uses service invocation to call Kitchen and Delivery (hybrid: pub/sub for status updates, service invocation for workflow)

---

### 03b - Dapr Pub/Sub (Programmatic Subscription)

**Purpose:** Same as 03a but demonstrates **programmatic subscriptions** (code-based).

**Services:** All 4 core services

**Key Difference from 03a:**
- `PizzaOrder` adds `app.UseCloudEvents()` AND `app.MapSubscribeHandler()`
- `OrderController.HandleOrderUpdate` is decorated with `[Topic("pizzapubsub", "orders")]` attribute
- No declarative `subscription.yaml` file needed

---

### 04 - Dapr Workflow

**Purpose:** Introduces Dapr **Workflow** to orchestrate the entire pizza lifecycle as a durable workflow.

**Services:** All 4 core services + **PizzaWorkflow** (5 services total)

**Technologies:**
- Dapr Workflow (`Dapr.Workflow` NuGet — `AddDaprWorkflow()`)
- Dapr Service Invocation (workflow activities call other services)
- Dapr Pub/Sub (status updates)
- External Events (human-in-the-loop validation)

**Key Code:**
- **`PizzaOrderingWorkflow`** — Durable workflow with 4 steps:
  1. `StorefrontActivity` — Calls `pizza-storefront`/`/storefront/order` via service invocation
  2. `CookingActivity` — Calls `pizza-kitchen`/`cook` via service invocation
  3. `ValidationActivity` — Saves validation state; workflow pauses with `WaitForExternalEventAsync<ValidationRequest>("ValidationComplete")`
  4. `DeliveryActivity` — Calls `pizza-delivery`/`delivery` via service invocation
- **`WorkflowController`** — REST API for workflow management:
  - `POST /workflow/start-order` — Starts new workflow instance
  - `POST /workflow/validate-pizza` — Raises `ValidationComplete` external event
  - `POST /workflow/get-status` — Gets workflow state
  - `POST /workflow/pause-order` / `resume-order` / `cancel-order`

---

### 05 - .NET Aspire Basics

**Purpose:** Introduces **.NET Aspire** as orchestrator for Dapr services.

**Services:** PizzaOrder + Aspire AppHost + Dapr Dashboard

**New Projects:**
- `DaprWorkshop.AppHost` — Aspire orchestrator (SDK `Aspire.AppHost.Sdk/13.1.2`)
- `DaprWorkshop.ServiceDefaults` — Shared Aspire configuration

**Technologies:**
- .NET Aspire (`DistributedApplication`)
- `CommunityToolkit.Aspire.Hosting.Dapr` for Dapr sidecar integration
- OpenTelemetry (tracing, metrics, logging via OTLP)
- Service Discovery, HTTP Resilience (Polly), Health Checks (`/health`, `/alive`)

**ServiceDefaults (`Extensions.cs`):**
- `AddServiceDefaults()` — Configures OpenTelemetry, health checks, service discovery, HTTP resilience
- `MapDefaultEndpoints()` — `/health` and `/alive` endpoints

---

### 06 - Aspire with Service Invocation

**Purpose:** Extends Aspire orchestration to all 4 core services with Dapr service invocation.

**Services:** All 4 core services + Aspire AppHost + Dapr Dashboard

**AppHost:** Registers all 4 services with Dapr sidecars via `AddProject<T>().WithDaprSidecar()`

---

### 07 - Aspire with Pub/Sub

**Purpose:** Adds Dapr pub/sub to the Aspire solution. Combines service invocation + pub/sub + state management.

**Services:** All 4 core services + Aspire AppHost + Dapr Dashboard

**Key Changes:**
- All sidecars get `ResourcesPaths` pointing to pubsub + statestore YAML
- Programmatic subscriptions with `[Topic("pizzapubsub", "orders")]`
- All services publish status updates via pub/sub

---

### 08 - Aspire with Dapr Workflow

**Purpose:** Adds the Dapr Workflow service to the Aspire-orchestrated solution.

**Services:** All 5 services + Aspire AppHost + Dapr Dashboard

**Technologies:** Everything from 07 plus Dapr Workflow with synchronous activities (same as challenge 04).

---

### 09 - Aspire with Event-Driven Workflow (Fully Decoupled)

**Purpose:** The most advanced challenge. Refactors the workflow to be **fully event-driven** — activities publish messages via pub/sub instead of synchronous service invocation.

**Services:** All 5 services + **PizzaShared** library + Aspire AppHost + Dapr Dashboard

**Critical Architecture Change:**
1. Workflow activities publish a message to a service-specific **topic** (e.g., `storefront`, `kitchen`, `delivery`)
2. Services subscribe to their topic, process the request, and publish a **result message** to a workflow-specific topic (e.g., `workflow-storefront`, `workflow-kitchen`, `workflow-delivery`)
3. `WorkflowController` subscribes to workflow result topics and raises **external events** back to the workflow instance
4. Workflow uses `WaitForExternalEventAsync` to await results

**PizzaShared Library:**
- `WorkflowMessage` (abstract base) — includes `WorkflowId` for correlation
- `OrderMessage` / `OrderResultMessage`, `CookMessage` / `CookResultMessage`, `DeliverMessage` / `DeliverResultMessage`
- `MessageHelper` — Conversion between `Order` and typed workflow messages

---

## Dapr Building Blocks per Challenge

| Challenge | State Store | Service Invocation | Pub/Sub | Workflow |
|---|---|---|---|---|
| 01 | Yes | - | - | - |
| 02 | Yes | Yes | - | - |
| 03a | Yes | Yes | Yes (declarative) | - |
| 03b | Yes | Yes | Yes (programmatic) | - |
| 04 | Yes | Yes | Yes | Yes (synchronous) |
| 05 | Yes | - | - | - |
| 06 | Yes | Yes | - | - |
| 07 | Yes | Yes | Yes (programmatic) | - |
| 08 | Yes | Yes | Yes (programmatic) | Yes (synchronous) |
| 09 | Yes | - | Yes (event-driven) | Yes (asynchronous) |

---

## Shared Directories

### `shared/`
Dapr component configuration for challenges 01-04:
- `resources-simple/statestore.yaml` — Redis state store (no scopes)
- `resources/pubsub.yaml` — Redis pub/sub (scoped to all 5 services)
- `resources/statestore.yaml` — Redis state store (with actor support, scoped)
- `resources-declarative/` — Includes declarative `subscription.yaml`
- `Endpoints.http` — HTTP test file with requests for all services

### `aspire-shared/`
Dapr component configuration for Aspire challenges 05-08:
- `resources-simple/statestore.yaml` — Redis state store (no scopes)
- `resources/pubsub.yaml` — Redis pub/sub (scoped)
- `resources/statestore.yaml` — Redis state store (scoped, actor-enabled)

### `Update-AspirePackages.ps1`
PowerShell script for centrally updating NuGet package versions. Supports `-TargetVersion` for pinning and `-DryRun` for preview.

---

## Key Patterns

### 1. Progressive Complexity
Challenges build incrementally: State → Service Invocation → Pub/Sub → Workflow → Aspire (repeats the progression) → Event-Driven.

### 2. Controller + Service Pattern
All services follow the same architecture:
- `Controllers/` — Thin controllers that delegate to services
- `Services/` — Business logic with interfaces for DI (`IStorefrontService`, `ICookService`, `IDeliveryService`, `IOrderStateService`)
- `Models/` — Data transfer objects (`Order`, `Customer`, `ValidationRequest`)

### 3. Multi-Stage Simulation
Each service simulates real-world processing with named stages and configurable delays:
- **Storefront:** validating → processing → confirmed
- **Kitchen:** preparing_ingredients → making_dough → adding_toppings → baking → quality_check → cooked
- **Delivery:** finding_driver → driver_assigned → picked_up → on_the_way → arriving → at_location → delivered

### 4. Two Subscription Approaches
- **Declarative (03a):** Subscription defined in YAML, no code changes needed
- **Programmatic (03b + all Aspire challenges):** `[Topic(...)]` attribute + `app.MapSubscribeHandler()`

### 5. Two Workflow Orchestration Approaches
- **Synchronous (04, 08):** Activities call services directly via service invocation
- **Event-Driven (09):** Activities publish messages, services process asynchronously, workflow waits for external events with `WorkflowId` correlation

### 6. Aspire Integration Pattern
- `DaprWorkshop.AppHost` — Registers services with `AddProject<T>().WithDaprSidecar()`
- `DaprWorkshop.ServiceDefaults` — OpenTelemetry, health checks, service discovery, HTTP resilience
- Each service calls `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()`

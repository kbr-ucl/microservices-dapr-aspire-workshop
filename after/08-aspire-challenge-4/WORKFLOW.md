# Pizza Ordering Workflow Documentation

## Overview

This solution implements a **pizza ordering system** using a microservices architecture orchestrated by [Dapr Workflow](https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-overview/) and [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/). The `PizzaOrderingWorkflow` drives the entire order lifecycle — from placement through cooking, validation, and delivery — coordinating five independent services via Dapr building blocks (service invocation, pub/sub, state management).

---

## Architecture

```
                         ┌──────────────────────────────────────────────────┐
                         │            .NET Aspire AppHost                   │
                         │  (orchestrates all services + Dapr sidecars)     │
                         └──────────────────────────────────────────────────┘
                                              │
          ┌───────────────┬───────────────┬───┴───────────┬────────────────┐
          ▼               ▼               ▼               ▼                ▼
  ┌──────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
  │ PizzaWorkflow│ │  Pizza      │ │  Pizza      │ │  Pizza      │ │  Pizza      │
  │  (port 3505) │ │  Storefront │ │  Kitchen    │ │  Delivery   │ │  Order      │
  │              │ │  (port 3502)│ │  (port 3503)│ │  (port 3504)│ │  (port 3501)│
  └──────┬───────┘ └──────┬──────┘ └──────┬──────┘ └──────┬──────┘ └──────┬──────┘
         │                │               │               │               │
         │     Dapr Service Invocation    │               │               │
         │────────────────►               │               │               │
         │────────────────────────────────►               │               │
         │────────────────────────────────────────────────►               │
         │                │               │               │               │
         │                │    Dapr Pub/Sub ("pizzapubsub" / "orders")    │
         │                │───────────────┼───────────────┼──────────────►│
         │                │               │───────────────┼──────────────►│
         │                │               │               │──────────────►│
         │                │               │               │               │
         └────────────────┴───────────────┴───────────────┴───────────────┘
                                          │
                              ┌───────────┴───────────┐
                              │   Redis (localhost)    │
                              │  • State Store         │
                              │  • Pub/Sub Broker      │
                              └───────────────────────┘
```

---

## Services

| Service | Dapr App ID | Dapr HTTP Port | Responsibility |
|---|---|---|---|
| **PizzaWorkflow** | `pizza-workflow` | 3505 | Orchestrates the entire order lifecycle via Dapr Workflow |
| **PizzaStorefront** | `pizza-storefront` | 3502 | Validates and confirms incoming orders |
| **PizzaKitchen** | `pizza-kitchen` | 3503 | Cooks the pizza through multiple preparation stages |
| **PizzaDelivery** | `pizza-delivery` | 3504 | Delivers the finished pizza to the customer |
| **PizzaOrder** | `pizza-order` | 3501 | Maintains order state; subscribes to status updates via pub/sub |

---

## Dapr Building Blocks Used

| Building Block | Component Name | Type | Purpose |
|---|---|---|---|
| **Workflow** | — | Built-in | Orchestrates the multi-step pizza order process |
| **Service Invocation** | — | Built-in | Synchronous calls between workflow activities and services |
| **Pub/Sub** | `pizzapubsub` | `pubsub.redis` | Broadcasts order status updates to the Order service |
| **State Store** | `pizzastatestore` | `state.redis` | Persists order state and validation state |

---

## Workflow: `PizzaOrderingWorkflow`

The core workflow is defined in `PizzaWorkflow/Workflows/PizzaOrderingWorkflow.cs`. It accepts an `Order` and returns an `Order` with the final status.

### Workflow Steps

```
  ┌─────────────────────┐
  │  1. StorefrontActivity │  ──► Invokes pizza-storefront to validate & confirm the order
  └──────────┬──────────┘
             │ status == "confirmed"
             ▼
  ┌─────────────────────┐
  │  2. CookingActivity    │  ──► Invokes pizza-kitchen to cook the pizza
  └──────────┬──────────┘
             │ status == "cooked"
             ▼
  ┌─────────────────────┐
  │  3. ValidationActivity │  ──► Saves "pending_validation" state; pauses workflow
  └──────────┬──────────┘
             │ WaitForExternalEvent("ValidationComplete")
             │ (human / manager approval required)
             ▼
  ┌─────────────────────┐
  │  4. DeliveryActivity   │  ──► Invokes pizza-delivery to deliver the pizza
  └──────────┬──────────┘
             │ status == "delivered"
             ▼
        status = "completed"
```

### Step Details

#### Step 1 — Storefront (Place & Process Order)
- **Activity:** `StorefrontActivity`
- **Dapr call:** Service invocation → `pizza-storefront` → `POST /storefront/order`
- **What happens:** The Storefront service processes the order through three stages:
  1. `validating` (1 s) — validates order data
  2. `processing` (2 s) — processes the order
  3. `confirmed` (1 s) — confirms the order
- **Pub/Sub:** Each status change is published to the `orders` topic on `pizzapubsub`
- **Success gate:** Order status must be `"confirmed"` to proceed

#### Step 2 — Kitchen (Cook the Pizza)
- **Activity:** `CookingActivity`
- **Dapr call:** Service invocation → `pizza-kitchen` → `POST /cook`
- **What happens:** The Kitchen service simulates cooking through five stages:
  1. `cooking_preparing_ingredients` (2 s)
  2. `cooking_making_dough` (3 s)
  3. `cooking_adding_toppings` (2 s)
  4. `cooking_baking` (5 s)
  5. `cooking_quality_check` (1 s)
- **Pub/Sub:** Each status change is published to the `orders` topic
- **Success gate:** Order status must be `"cooked"` to proceed

#### Step 3 — Validation (Manager Approval)
- **Activity:** `ValidationActivity`
- **What happens:**
  1. Order status is set to `"waiting_for_validation"`
  2. Validation state is saved to the `pizzastatestore` state store
  3. **The workflow pauses** and waits for an external event: `"ValidationComplete"`
- **Human interaction required:** A manager must call `POST /workflow/validate-pizza` with `{ orderId, approved: true/false }`
- **Success gate:** `ValidationRequest.Approved` must be `true` to proceed

#### Step 4 — Delivery (Deliver the Pizza)
- **Activity:** `DeliveryActivity`
- **Dapr call:** Service invocation → `pizza-delivery` → `POST /delivery`
- **What happens:** The Delivery service simulates delivery through six stages:
  1. `delivery_finding_driver` (2 s)
  2. `delivery_driver_assigned` (1 s)
  3. `delivery_picked_up` (2 s)
  4. `delivery_on_the_way` (5 s)
  5. `delivery_arriving` (2 s)
  6. `delivery_at_location` (1 s)
- **Pub/Sub:** Each status change is published to the `orders` topic
- **Success gate:** Order status must be `"delivered"` to proceed
- **Final status:** Set to `"completed"`

### Error Handling

If any step fails or a gate check is not met, the workflow catches the exception and returns the order with:
- `Status = "failed"`
- `Error = <exception message>`

---

## Workflow Management API

The `WorkflowController` (`PizzaWorkflow/Controllers/WorkflowController.cs`) exposes endpoints to manage workflow instances:

| Endpoint | Method | Body | Description |
|---|---|---|---|
| `/workflow/start-order` | POST | `Order` | Starts a new `PizzaOrderingWorkflow` instance (ID: `pizza-order-{orderId}`) |
| `/workflow/validate-pizza` | POST | `ValidationRequest` | Raises the `"ValidationComplete"` external event to resume the paused workflow |
| `/workflow/get-status` | POST | `ManageWorkflowRequest` | Retrieves the current workflow state |
| `/workflow/pause-order` | POST | `ManageWorkflowRequest` | Suspends a running workflow |
| `/workflow/resume-order` | POST | `ManageWorkflowRequest` | Resumes a suspended workflow |
| `/workflow/cancel-order` | POST | `ManageWorkflowRequest` | Terminates a workflow |

---

## Order State Tracking (PizzaOrder Service)

The `PizzaOrder` service acts as the **single source of truth** for order state:

- **Subscribes** to the `orders` topic on `pizzapubsub` via `POST /orders-sub` (Dapr programmatic subscription with `[Topic("pizzapubsub", "orders")]`)
- **Persists** every status update to the `pizzastatestore` Redis state store (key: `order_{orderId}`)
- **Exposes** CRUD endpoints:
  - `POST /order` — create/update an order
  - `GET /order/{orderId}` — retrieve order state
  - `DELETE /order/{orderId}` — delete an order

This means every status change published by Storefront, Kitchen, and Delivery is automatically captured and persisted.

---

## Data Models

### `Order`
| Property | Type | Description |
|---|---|---|
| `OrderId` | `string` | Unique order identifier |
| `PizzaType` | `string` | Type of pizza (e.g., "Margherita") |
| `Size` | `string` | Pizza size (e.g., "Large") |
| `Customer` | `Customer` | Customer details |
| `Status` | `string` | Current order status (default: `"created"`) |
| `Error` | `string?` | Error message if the order failed |

### `Customer`
| Property | Type | Description |
|---|---|---|
| `Name` | `string` | Customer name |
| `Address` | `string` | Delivery address |
| `Phone` | `string` | Contact phone number |

### `ValidationRequest`
| Property | Type | Description |
|---|---|---|
| `OrderId` | `string` | Order to validate |
| `Approved` | `bool` | Whether the pizza passes quality validation |

### `ManageWorkflowRequest`
| Property | Type | Description |
|---|---|---|
| `OrderId` | `string` | Order whose workflow to manage |

---

## Complete Status Lifecycle

```
created
  → validating → processing → confirmed          (Storefront)
  → cooking_preparing_ingredients                 (Kitchen)
  → cooking_making_dough
  → cooking_adding_toppings
  → cooking_baking
  → cooking_quality_check
  → cooked
  → waiting_for_validation                        (Validation — workflow pauses)
  → [ValidationComplete event received]
  → delivery_finding_driver                       (Delivery)
  → delivery_driver_assigned
  → delivery_picked_up
  → delivery_on_the_way
  → delivery_arriving
  → delivery_at_location
  → delivered
  → completed                                     (Workflow sets final status)
```

**Failure at any point** → `failed`

---

## Infrastructure

- **Orchestrator:** .NET Aspire (`DaprWorkshop.AppHost`)
- **Service Defaults:** Shared project providing OpenTelemetry, health checks, and service discovery
- **Dapr Sidecars:** Each service runs with a Dapr sidecar configured via `CommunityToolkit.Aspire.Hosting.Dapr`
- **Redis:** Used as both the pub/sub message broker and the state store (localhost:6379)
- **Dapr Dashboard:** Available at `http://localhost:8080` for runtime observability
- **API Documentation:** Each service exposes OpenAPI + Scalar API reference in development mode

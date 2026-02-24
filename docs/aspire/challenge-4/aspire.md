# Aspire - Challenge 4 - Workflow
## Prepare

Navigate to the `before/08-aspire-challenge-4` folder 


### Add  .NET Aspire Orchestration Support

- Right click `PizzaStorefront`project and add  `.NET Aspire Orchestration Support`

- Right click `PizzaKitchen`project and add  `.NET Aspire Orchestration Support`

- Right click `PizzaDelivery`project and add  `.NET Aspire Orchestration Support`

- Right click `PizzaOrder`project and add  `.NET Aspire Orchestration Support`

- Right click `PizzaWorkflow`project and add  `.NET Aspire Orchestration Support`



## Dapr Resources

Aspire version 1.15.4 uses Dapr resources files.

We will use Programmatic Pub/sub API subscription (Link: [Pub/sub API subscription types](https://docs.dapr.io/developing-applications/building-blocks/pubsub/subscription-methods/#pubsub-api-subscription-types) )



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

| Service             | Dapr App ID        | Dapr HTTP Port | Responsibility                                               |
| ------------------- | ------------------ | -------------- | ------------------------------------------------------------ |
| **PizzaWorkflow**   | `pizza-workflow`   | 3505           | Orchestrates the entire order lifecycle via Dapr Workflow    |
| **PizzaStorefront** | `pizza-storefront` | 3502           | Validates and confirms incoming orders                       |
| **PizzaKitchen**    | `pizza-kitchen`    | 3503           | Cooks the pizza through multiple preparation stages          |
| **PizzaDelivery**   | `pizza-delivery`   | 3504           | Delivers the finished pizza to the customer                  |
| **PizzaOrder**      | `pizza-order`      | 3501           | Maintains order state; subscribes to status updates via pub/sub |

---

## Dapr Building Blocks Used

| Building Block         | Component Name    | Type           | Purpose                                                    |
| ---------------------- | ----------------- | -------------- | ---------------------------------------------------------- |
| **Workflow**           | —                 | Built-in       | Orchestrates the multi-step pizza order process            |
| **Service Invocation** | —                 | Built-in       | Synchronous calls between workflow activities and services |
| **Pub/Sub**            | `pizzapubsub`     | `pubsub.redis` | Broadcasts order status updates to the Order service       |
| **State Store**        | `pizzastatestore` | `state.redis`  | Persists order state and validation state                  |

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



## Hosting integration

In your .NET Aspire solution, to integrate Dapr and access its types and APIs, add the CommunityToolkit.Aspire.Hosting.Dapr NuGet package in the `DaprWorkshop.AppHost` project.

```powershell
dotnet add package CommunityToolkit.Aspire.Hosting.Dapr
```

### Nuget update

Use Nuget Packet Manager for solution to update packets.

### In folder DaprWorkshop.AppHost add changes to `Program.cs`

#### Add Dapr resources

```c#
using CommunityToolkit.Aspire.Hosting.Dapr;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var daprResources = ImmutableHashSet.Create("../../aspire-shared/resources-simple");
```

#### Add Dapr sidecar to .NET Aspire resources

Dapr uses the [sidecar pattern](https://docs.dapr.io/concepts/dapr-services/sidecar/). The Dapr sidecar runs alongside your app as a lightweight, portable, and stateless HTTP server that listens for incoming HTTP requests from your app.

To add a sidecar to a .NET Aspire resource, call the [WithDaprSidecar](https://learn.microsoft.com/en-us/dotnet/api/aspire.hosting.idistributedapplicationresourcebuilderextensions.withdaprsidecar) method on it. The `appId` parameter is the unique identifier for the Dapr application, but it's optional. If you don't provide an `appId`, the parent resource name is used instead. Also add reference to `ResourcesPaths`  to include the Dapr resources yaml files.

```c#
using CommunityToolkit.Aspire.Hosting.Dapr;
using Projects;
using System.Collections.Immutable;

var builder = DistributedApplication.CreateBuilder(args);
var daprResources = ImmutableHashSet.Create("../../aspire-shared/resources");

builder.AddProject<PizzaOrder>("pizzaorderservice")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "pizza-order",
        DaprHttpPort = 3501,
        ResourcesPaths = daprResources
    });


builder.AddProject<PizzaKitchen>("pizzakitchenservice")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "pizza-kitchen",
        DaprHttpPort = 3503,
        ResourcesPaths = daprResources
    });


builder.AddProject<PizzaStorefront>("pizzastorefrontservice")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "pizza-storefront",
        DaprHttpPort = 3502,
        ResourcesPaths = daprResources
    });


builder.AddProject<PizzaDelivery>("pizzadeliveryservice")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "pizza-delivery",
        DaprHttpPort = 3504,
        ResourcesPaths = daprResources
    });


builder.AddProject<PizzaWorkflow>("pizzaworkflowservice")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "pizza-workflow",
        DaprHttpPort = 3505,
        ResourcesPaths = daprResources
    });


builder.AddExecutable("dapr-dashboard", "dapr", ".", "dashboard")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, isProxied: false);

builder.Build().Run();
```

#### Add Dapr Dashboard

The [Dapr Dashboard](https://docs.dapr.io/reference/dashboard/) is a web-based UI that provides information about Dapr applications, components, and configurations. By adding it as an executable resource in Aspire, the dashboard is launched automatically and accessible from the Aspire dashboard at `http://localhost:8080`.

## Application changes

Since we are using Aspire component files (resources) we can have to return switch Dapr programmatic PubSub. 

### Changes in `PizzaOrder`

#### Changes to `OrderController.cs`

Change `HandleOrderUpdate` to programmatic pub/sub 

```c#
    [HttpPost("/orders-sub")]
    [Topic("pizzapubsub", "orders")] // Programmatic Dapr pub/sub topic 
    public async Task<IActionResult> HandleOrderUpdate(Order cloudEvent)
    {
        _logger.LogInformation("Received order update for order {OrderId}", 
            cloudEvent.OrderId);

        var result = await _orderStateService.UpdateOrderStateAsync(cloudEvent);
        return Ok();
    }
}
```



#### In program.cs:

```c#
using PizzaOrder.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers().AddDapr();
builder.Services.AddSingleton<IOrderStateService, OrderStateService>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Dapr will send serialized event object vs. being raw CloudEvent
app.UseCloudEvents();

// Needed for Programmatic Dapr pub/sub routing
app.MapSubscribeHandler();

app.MapControllers();
app.Run();
```



#### In OrderController.cs

```
    [HttpPost("/orders-sub")]
    [Topic("pizzapubsub", "orders")]
    public async Task<IActionResult> HandleOrderUpdate(Order cloudEvent)
    {
        _logger.LogInformation("Received order update for order {OrderId}", 
            cloudEvent.OrderId);

        var result = await _orderStateService.UpdateOrderStateAsync(cloudEvent);
        return Ok();
    }
```



#### CloudEvent.cs

Delete `CloudEvent`



## Workflow Management API

The `WorkflowController` (`PizzaWorkflow/Controllers/WorkflowController.cs`) exposes endpoints to manage workflow instances:

| Endpoint                   | Method | Body                    | Description                                                  |
| -------------------------- | ------ | ----------------------- | ------------------------------------------------------------ |
| `/workflow/start-order`    | POST   | `Order`                 | Starts a new `PizzaOrderingWorkflow` instance (ID: `pizza-order-{orderId}`) |
| `/workflow/validate-pizza` | POST   | `ValidationRequest`     | Raises the `"ValidationComplete"` external event to resume the paused workflow |
| `/workflow/get-status`     | POST   | `ManageWorkflowRequest` | Retrieves the current workflow state                         |
| `/workflow/pause-order`    | POST   | `ManageWorkflowRequest` | Suspends a running workflow                                  |
| `/workflow/resume-order`   | POST   | `ManageWorkflowRequest` | Resumes a suspended workflow                                 |
| `/workflow/cancel-order`   | POST   | `ManageWorkflowRequest` | Terminates a workflow                                        |

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

| Property    | Type       | Description                                 |
| ----------- | ---------- | ------------------------------------------- |
| `OrderId`   | `string`   | Unique order identifier                     |
| `PizzaType` | `string`   | Type of pizza (e.g., "Margherita")          |
| `Size`      | `string`   | Pizza size (e.g., "Large")                  |
| `Customer`  | `Customer` | Customer details                            |
| `Status`    | `string`   | Current order status (default: `"created"`) |
| `Error`     | `string?`  | Error message if the order failed           |

### `Customer`

| Property  | Type     | Description          |
| --------- | -------- | -------------------- |
| `Name`    | `string` | Customer name        |
| `Address` | `string` | Delivery address     |
| `Phone`   | `string` | Contact phone number |

### `ValidationRequest`

| Property   | Type     | Description                                 |
| ---------- | -------- | ------------------------------------------- |
| `OrderId`  | `string` | Order to validate                           |
| `Approved` | `bool`   | Whether the pizza passes quality validation |

### `ManageWorkflowRequest`

| Property  | Type     | Description                    |
| --------- | -------- | ------------------------------ |
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

## Test the service (Postman)

Open Postman and import `microservices-dapr-aspire-workshop.postman_collection.json`

Execute `Start a new pizza order workflow`

Wait for the output line (in Aspire Console logs) `Order 1 cooked with status cooked` 

Then execute `Validate pizza (approve)`

In Aspire Dashboard: Navigate to the `pizzaworkflowservice` Console logs, where you should see the following logs:

```
2025-02-05T19:32:33 info: Dapr.Workflow.WorkflowLoggingService[0]
2025-02-05T19:32:33       WorkflowLoggingService started
2025-02-05T19:32:33 info: Dapr.Workflow.WorkflowLoggingService[0]
2025-02-05T19:32:33       List of registered workflows
2025-02-05T19:32:33 info: Dapr.Workflow.WorkflowLoggingService[0]
2025-02-05T19:32:33       PizzaOrderingWorkflow
2025-02-05T19:32:33 info: Dapr.Workflow.WorkflowLoggingService[0]
2025-02-05T19:32:33       List of registered activities:
2025-02-05T19:32:33 info: Dapr.Workflow.WorkflowLoggingService[0]
2025-02-05T19:32:33       StorefrontActivity
2025-02-05T19:32:33 info: Dapr.Workflow.WorkflowLoggingService[0]
2025-02-05T19:32:33       CookingActivity
2025-02-05T19:32:33 info: Dapr.Workflow.WorkflowLoggingService[0]
2025-02-05T19:32:33       ValidationActivity
2025-02-05T19:32:33 info: Dapr.Workflow.WorkflowLoggingService[0]
2025-02-05T19:32:33       DeliveryActivity
2025-02-05T19:32:33 info: Microsoft.DurableTask[1]
2025-02-05T19:32:33       Durable Task gRPC worker starting.
2025-02-05T19:32:33 info: Microsoft.Hosting.Lifetime[14]
2025-02-05T19:32:33       Now listening on: http://localhost:58245
2025-02-05T19:32:33 info: Microsoft.Hosting.Lifetime[0]
2025-02-05T19:32:33       Application started. Press Ctrl+C to shut down.
2025-02-05T19:32:33 info: Microsoft.Hosting.Lifetime[0]
2025-02-05T19:32:33       Hosting environment: Development
2025-02-05T19:32:33 info: Microsoft.Hosting.Lifetime[0]
2025-02-05T19:32:33       Content root path: C:\Dropbox\SourceCode\dapr\02-DaprAspire\dapr-workshop-aspire\start-here\PizzaWorkflow
2025-02-05T19:32:33 info: Microsoft.DurableTask[4]
2025-02-05T19:32:33       Sidecar work-item streaming connection established.
2025-02-05T19:32:35 info: PizzaWorkflow.Controllers.WorkflowController[0]
2025-02-05T19:32:35       Starting workflow for order 1
2025-02-05T19:32:36 info: PizzaWorkflow.Controllers.WorkflowController[0]
2025-02-05T19:32:36       Workflow started successfully for order 1
2025-02-05T19:32:36 info: PizzaWorkflow.Activities.StorefrontActivity[0]
2025-02-05T19:32:36       Starting ordering process for order 1
2025-02-05T19:32:41 info: PizzaWorkflow.Activities.StorefrontActivity[0]
2025-02-05T19:32:41       Order 1 processed with status confirmed
2025-02-05T19:32:41 info: PizzaWorkflow.Activities.CookingActivity[0]
2025-02-05T19:32:41       Starting cooking process for order 1
2025-02-05T19:32:55 info: PizzaWorkflow.Activities.CookingActivity[0]
2025-02-05T19:32:55       Order 1 cooked with status cooked
2025-02-05T19:32:55 info: PizzaWorkflow.Activities.ValidationActivity[0]
2025-02-05T19:32:55       Starting validation process for order 1
2025-02-05T19:32:55 info: PizzaWorkflow.Activities.ValidationActivity[0]
2025-02-05T19:32:55       Validation state saved for order 1
2025-02-05T19:33:00 info: PizzaWorkflow.Controllers.WorkflowController[0]
2025-02-05T19:33:00       Raising validation event for order 1. Approved: True
2025-02-05T19:33:00 info: PizzaWorkflow.Controllers.WorkflowController[0]
2025-02-05T19:33:00       Validation event raised successfully for order 1
2025-02-05T19:33:00 info: PizzaWorkflow.Activities.DeliveryActivity[0]
2025-02-05T19:33:00       Starting delivery process for order 1
2025-02-05T19:33:13 info: PizzaWorkflow.Activities.DeliveryActivity[0]
2025-02-05T19:33:13       Order 1 delivered with status delivered
```

In Aspire Dashboard: Navigate to the `pizzaorderservice` Console logs, where you should see the following logs:

```
2025-02-05T19:32:32 info: Microsoft.Hosting.Lifetime[14]
2025-02-05T19:32:32       Now listening on: http://localhost:58238
2025-02-05T19:32:32 info: Microsoft.Hosting.Lifetime[0]
2025-02-05T19:32:32       Application started. Press Ctrl+C to shut down.
2025-02-05T19:32:32 info: Microsoft.Hosting.Lifetime[0]
2025-02-05T19:32:32       Hosting environment: Development
2025-02-05T19:32:32 info: Microsoft.Hosting.Lifetime[0]
2025-02-05T19:32:32       Content root path: C:\Dropbox\SourceCode\dapr\02-DaprAspire\dapr-workshop-aspire\start-here\PizzaOrder
2025-02-05T19:32:37 info: PizzaOrder.Controllers.OrderController[0]
2025-02-05T19:32:37       Received order update for order 1
2025-02-05T19:32:37 info: PizzaOrder.Services.OrderStateService[0]
2025-02-05T19:32:37       Updated state for order 1 - Status: validating
2025-02-05T19:32:38 info: PizzaOrder.Controllers.OrderController[0]
2025-02-05T19:32:38       Received order update for order 1
2025-02-05T19:32:38 info: PizzaOrder.Services.OrderStateService[0]
2025-02-05T19:32:38       Updated state for order 1 - Status: processing
2025-02-05T19:32:40 info: PizzaOrder.Controllers.OrderController[0]
2025-02-05T19:32:40       Received order update for order 1
2025-02-05T19:32:40 info: PizzaOrder.Services.OrderStateService[0]
2025-02-05T19:32:40       Updated state for order 1 - Status: confirmed
2025-02-05T19:32:41 info: PizzaOrder.Controllers.OrderController[0]
2025-02-05T19:32:41       Received order update for order 1
2025-02-05T19:32:41 info: PizzaOrder.Services.OrderStateService[0]
2025-02-05T19:32:41       Updated state for order 1 - Status: cooking_preparing_ingredients
2025-02-05T19:32:43 info: PizzaOrder.Controllers.OrderController[0]
2025-02-05T19:32:43       Received order update for order 1
2025-02-05T19:32:43 info: PizzaOrder.Services.OrderStateService[0]
2025-02-05T19:32:43       Updated state for order 1 - Status: cooking_making_dough
2025-02-05T19:32:46 info: PizzaOrder.Controllers.OrderController[0]
2025-02-05T19:32:46       Received order update for order 1
2025-02-05T19:32:46 info: PizzaOrder.Services.OrderStateService[0]
2025-02-05T19:32:46       Updated state for order 1 - Status: cooking_adding_toppings
2025-02-05T19:32:48 info: PizzaOrder.Controllers.OrderController[0]
2025-02-05T19:32:48       Received order update for order 1
2025-02-05T19:32:49 info: PizzaOrder.Services.OrderStateService[0]
2025-02-05T19:32:49       Updated state for order 1 - Status: cooking_baking
2025-02-05T19:32:54 info: PizzaOrder.Controllers.OrderController[0]
2025-02-05T19:32:54       Received order update for order 1
2025-02-05T19:32:54 info: PizzaOrder.Services.OrderStateService[0]
2025-02-05T19:32:54       Updated state for order 1 - Status: cooking_quality_check
2025-02-05T19:32:55 info: PizzaOrder.Controllers.OrderController[0]
2025-02-05T19:32:55       Received order update for order 1
2025-02-05T19:32:55 info: PizzaOrder.Services.OrderStateService[0]
2025-02-05T19:32:55       Updated state for order 1 - Status: cooked
2025-02-05T19:33:00 info: PizzaOrder.Controllers.OrderController[0]
2025-02-05T19:33:00       Received order update for order 1
2025-02-05T19:33:00 info: PizzaOrder.Services.OrderStateService[0]
2025-02-05T19:33:00       Updated state for order 1 - Status: delivery_finding_driver
2025-02-05T19:33:02 info: PizzaOrder.Controllers.OrderController[0]
2025-02-05T19:33:02       Received order update for order 1
2025-02-05T19:33:02 info: PizzaOrder.Services.OrderStateService[0]
2025-02-05T19:33:02       Updated state for order 1 - Status: delivery_driver_assigned
2025-02-05T19:33:03 info: PizzaOrder.Controllers.OrderController[0]
2025-02-05T19:33:03       Received order update for order 1
2025-02-05T19:33:03 info: PizzaOrder.Services.OrderStateService[0]
2025-02-05T19:33:03       Updated state for order 1 - Status: delivery_picked_up
2025-02-05T19:33:05 info: PizzaOrder.Controllers.OrderController[0]
2025-02-05T19:33:05       Received order update for order 1
2025-02-05T19:33:05 info: PizzaOrder.Services.OrderStateService[0]
2025-02-05T19:33:05       Updated state for order 1 - Status: delivery_on_the_way
2025-02-05T19:33:10 info: PizzaOrder.Controllers.OrderController[0]
2025-02-05T19:33:10       Received order update for order 1
2025-02-05T19:33:10 info: PizzaOrder.Services.OrderStateService[0]
2025-02-05T19:33:10       Updated state for order 1 - Status: delivery_arriving
2025-02-05T19:33:12 info: PizzaOrder.Controllers.OrderController[0]
2025-02-05T19:33:12       Received order update for order 1
2025-02-05T19:33:12 info: PizzaOrder.Services.OrderStateService[0]
2025-02-05T19:33:12       Updated state for order 1 - Status: delivery_at_location
2025-02-05T19:33:13 info: PizzaOrder.Controllers.OrderController[0]
2025-02-05T19:33:13       Received order update for order 1
2025-02-05T19:33:13 info: PizzaOrder.Services.OrderStateService[0]
2025-02-05T19:33:13       Updated state for order 1 - Status: delivered
```



### Traces

In Aspire Dashboard: Navigate to Traces and select `pizzaworkflowservice: POST Workflow/start-order`

Observe the activities calls.



## Suggested Solution

You find a **Suggested Solution** in the `after` folder.

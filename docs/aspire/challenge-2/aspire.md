# Aspire - Challenge 2

## Prepare

Navigate to the `before/06-aspire-challenge-2` folder 

### Add  .NET Aspire Orchestration Support

- Right click `PizzaStorefront`project and add  `.NET Aspire Orchestration Support`

- Right click `PizzaKitchen`project and add  `.NET Aspire Orchestration Support`

- Right click `PizzaDelivery`project and add  `.NET Aspire Orchestration Support`





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

To add a sidecar to a .NET Aspire resource, call the [WithDaprSidecar](https://learn.microsoft.com/en-us/dotnet/api/aspire.hosting.idistributedapplicationresourcebuilderextensions.withdaprsidecar) method on it. The `appId` parameter is the unique identifier for the Dapr application, but it's optional. If you don't provide an `appId`, the parent resource name is used instead. Also add reference to `statestore`

```c#
using CommunityToolkit.Aspire.Hosting.Dapr;
using System.Collections.Immutable;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var daprResources = ImmutableHashSet.Create("../../aspire-shared/resources-simple");

builder.AddProject<PizzaOrder>("pizzaorderservice")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "pizza-order",
        DaprHttpPort = 3501,
        ResourcesPaths = daprResources
    });


builder.AddProject<PizzaStorefront>("pizzastorefrontservice")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "pizza-storefront",
        DaprHttpPort = 3502
    });


builder.AddProject<PizzaKitchen>("pizzakitchenservice")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "pizza-kitchen",
        DaprHttpPort = 3503
    });



builder.AddProject<PizzaDelivery>("pizzadeliveryservice")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "pizza-delivery",
        DaprHttpPort = 3504
    });

builder.AddExecutable("dapr-dashboard", "dapr", ".", "dashboard")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, isProxied: false);

builder.Build().Run();

```

#### Add Dapr Dashboard

The [Dapr Dashboard](https://docs.dapr.io/reference/dashboard/) is a web-based UI that provides information about Dapr applications, components, and configurations. By adding it as an executable resource in Aspire, the dashboard is launched automatically and accessible from the Aspire dashboard at `http://localhost:8080`.

## Test the service (Postman)

Open Postman and import `microservices-dapr-aspire-workshop.postman_collection.json`

Execute `Direct Pizza Store Endpoint`

In Aspire Dashboard: Navigate to the `pizzastorefrontservice` Console logs, where you should see the following logs:

```
2025-02-05T06:56:41 info: Microsoft.Hosting.Lifetime[14]
2025-02-05T06:56:41       Now listening on: http://localhost:51547
2025-02-05T06:56:41 info: Microsoft.Hosting.Lifetime[0]
2025-02-05T06:56:41       Application started. Press Ctrl+C to shut down.
2025-02-05T06:56:41 info: Microsoft.Hosting.Lifetime[0]
2025-02-05T06:56:41       Hosting environment: Development
2025-02-05T06:56:41 info: Microsoft.Hosting.Lifetime[0]
2025-02-05T06:56:41       Content root path: C:\Dropbox\SourceCode\dapr\DaprAspire\dapr-workshop-aspire\start-here\PizzaStorefront
2025-02-05T06:57:12 info: PizzaStorefront.Controllers.StorefrontController[0]
2025-02-05T06:57:12       Received new order: 123
2025-02-05T06:57:12 info: PizzaStorefront.Services.StorefrontService[0]
2025-02-05T06:57:12       Order 123 - validating
2025-02-05T06:57:13 info: PizzaStorefront.Services.StorefrontService[0]
2025-02-05T06:57:13       Order 123 - processing
2025-02-05T06:57:15 info: PizzaStorefront.Services.StorefrontService[0]
2025-02-05T06:57:15       Order 123 - confirmed
2025-02-05T06:57:16 info: PizzaStorefront.Services.StorefrontService[0]
2025-02-05T06:57:16       Starting cooking process for order 123
2025-02-05T06:57:29 info: PizzaStorefront.Services.StorefrontService[0]
2025-02-05T06:57:29       Order 123 cooked with status cooked
2025-02-05T06:57:29 info: PizzaStorefront.Services.StorefrontService[0]
2025-02-05T06:57:29       Starting delivery process for order 123
2025-02-05T06:57:43 info: PizzaStorefront.Services.StorefrontService[0]
2025-02-05T06:57:43       Order 123 delivered with status delivered
```

The logs for `pizzakitchenservice` should read:

```
2025-02-05T06:56:41 info: Microsoft.Hosting.Lifetime[14]
2025-02-05T06:56:41       Now listening on: http://localhost:51546
2025-02-05T06:56:41 info: Microsoft.Hosting.Lifetime[0]
2025-02-05T06:56:41       Application started. Press Ctrl+C to shut down.
2025-02-05T06:56:41 info: Microsoft.Hosting.Lifetime[0]
2025-02-05T06:56:41       Hosting environment: Development
2025-02-05T06:56:41 info: Microsoft.Hosting.Lifetime[0]
2025-02-05T06:56:41       Content root path: C:\Dropbox\SourceCode\dapr\DaprAspire\dapr-workshop-aspire\start-here\PizzaKitchen
2025-02-05T06:57:16 info: PizzaKitchen.Controllers.CookController[0]
2025-02-05T06:57:16       Starting cooking for order: 123
2025-02-05T06:57:16 info: PizzaKitchen.Services.CookService[0]
2025-02-05T06:57:16       Order 123 - cooking_preparing_ingredients
2025-02-05T06:57:18 info: PizzaKitchen.Services.CookService[0]
2025-02-05T06:57:18       Order 123 - cooking_making_dough
2025-02-05T06:57:21 info: PizzaKitchen.Services.CookService[0]
2025-02-05T06:57:21       Order 123 - cooking_adding_toppings
2025-02-05T06:57:23 info: PizzaKitchen.Services.CookService[0]
2025-02-05T06:57:23       Order 123 - cooking_baking
2025-02-05T06:57:28 info: PizzaKitchen.Services.CookService[0]
2025-02-05T06:57:28       Order 123 - cooking_quality_check
2025-02-05T06:57:29 info: PizzaKitchen.Services.CookService[0]
2025-02-05T06:57:29       Order 123 - cooked
```

Finally, on `pizzadeliveryservice`:

```
2025-02-05T06:56:41 info: Microsoft.Hosting.Lifetime[14]
2025-02-05T06:56:41       Now listening on: http://localhost:51549
2025-02-05T06:56:41 info: Microsoft.Hosting.Lifetime[0]
2025-02-05T06:56:41       Application started. Press Ctrl+C to shut down.
2025-02-05T06:56:41 info: Microsoft.Hosting.Lifetime[0]
2025-02-05T06:56:41       Hosting environment: Development
2025-02-05T06:56:41 info: Microsoft.Hosting.Lifetime[0]
2025-02-05T06:56:41       Content root path: C:\Dropbox\SourceCode\dapr\DaprAspire\dapr-workshop-aspire\start-here\PizzaDelivery
2025-02-05T06:57:30 info: PizzaDelivery.Controllers.DeliveryController[0]
2025-02-05T06:57:30       Starting delivery for order: 123
2025-02-05T06:57:30 info: PizzaDelivery.Services.DeliveryService[0]
2025-02-05T06:57:30       Order 123 - delivery_finding_driver
2025-02-05T06:57:32 info: PizzaDelivery.Services.DeliveryService[0]
2025-02-05T06:57:32       Order 123 - delivery_driver_assigned
2025-02-05T06:57:33 info: PizzaDelivery.Services.DeliveryService[0]
2025-02-05T06:57:33       Order 123 - delivery_picked_up
2025-02-05T06:57:35 info: PizzaDelivery.Services.DeliveryService[0]
2025-02-05T06:57:35       Order 123 - delivery_on_the_way
2025-02-05T06:57:40 info: PizzaDelivery.Services.DeliveryService[0]
2025-02-05T06:57:40       Order 123 - delivery_arriving
2025-02-05T06:57:42 info: PizzaDelivery.Services.DeliveryService[0]
2025-02-05T06:57:42       Order 123 - delivery_at_location
2025-02-05T06:57:43 info: PizzaDelivery.Services.DeliveryService[0]
2025-02-05T06:57:43       Order 123 - delivered
```

### Traces
In Aspire Dashboard: Navigate to Traces and select `pizzastorefrontservice: POST Storefront/order`

You should se something like this:

![aspire-challenge-2-01](assets/aspire-challenge-2-01.png)





## Suggested Solution

You find a **Suggested Solution** in the `after` folder.


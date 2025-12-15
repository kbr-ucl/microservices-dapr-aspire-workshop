# Aspire - Challenge 1

## Prepare

Navigate to the `before/05-aspire-challenge-1` folder 

### Add  .NET Aspire Orchestration Support

- Right click `PizzaOrder`project and add  `.NET Aspire Orchestration Support`



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
using System.Collections.Immutable;
using CommunityToolkit.Aspire.Hosting.Dapr;
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

builder.Build().Run();
```

## Test the service
Add the `Endpoints.http` in the `start-here` folder to the solution.

Open the `Endpoints.http` and place a new order by clicking the button `Send request` under `Direct Pizza Order Endpoint (for testing)`. Expected Body result:

```json
{
  "orderId": "123",
  "pizzaType": "pepperoni",
  "size": "large",
  "customer": {
    "name": "John Doe",
    "address": "123 Main St",
    "phone": "555-0123"
  },
  "status": "created",
  "error": null
}
```





## Suggested Solution

You find a **Suggested Solution** in the `after` folder.


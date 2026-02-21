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

builder.AddExecutable("dapr-dashboard", "dapr", ".", "dashboard")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, isProxied: false);

builder.Build().Run();
```

#### Add Dapr Dashboard

The [Dapr Dashboard](https://docs.dapr.io/reference/dashboard/) is a web-based UI that provides information about Dapr applications, components, and configurations. By adding it as an executable resource in Aspire, the dashboard is launched automatically and accessible from the Aspire dashboard at `http://localhost:8080`.



## Test the service (Postman)

Open Postman and import `microservices-dapr-aspire-workshop.postman_collection.json`

Execute `Direct Pizza Order Endpoint`

Expected Body result:

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


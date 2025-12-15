using CommunityToolkit.Aspire.Hosting.Dapr;
using Projects;
using System.Collections.Immutable;

var builder = DistributedApplication.CreateBuilder(args);
var daprResources = ImmutableHashSet.Create("../../shared/resources");

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

builder.Build().Run();
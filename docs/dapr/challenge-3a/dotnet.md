# Challenge 3a - Pub/Sub - Declarative

This tutorial shows how to implement Dapr declarative pub/sub  

## Introduction

Publish and subscribe (pub/sub) enables  microservices to asynchrony communicate with each other using messages for event-driven architectures with low temporal coupling.

- The producer, or **publisher**, writes messages to an input channel and sends them to a topic, unaware which application will receive them.
- The consumer, or **subscriber**, subscribes to the topic and receives messages from an output channel, unaware which service produced these messages.

An intermediary message broker copies each message from a publisher’s input channel to an output channel for all subscribers interested in that message. This pattern is especially useful when you need to decouple microservices from one another.

![pubsub-overview-pattern](assets/pubsub-overview-pattern.png)



### Pub/sub API

The pub/sub API in Dapr:

- Provides a platform-agnostic API to send and receive messages.
- Offers at-least-once message delivery guarantee.
- Integrates with various message brokers and queuing systems.

The specific message broker used by your service is pluggable and configured as a Dapr pub/sub component at runtime. This removes the dependency from your service and makes your service more portable and flexible to changes.

When using pub/sub in Dapr:

1. Your service makes a network call to a Dapr pub/sub building block API.
2. The pub/sub building block makes calls into a Dapr pub/sub component that encapsulates a specific message broker.
3. To receive messages on a topic, Dapr subscribes to the pub/sub component on behalf of your service with a topic and delivers the messages to an endpoint on your service when they arrive.



### Features

The pub/sub API building block brings several features to your application.

#### Sending messages using Cloud Events

To enable message routing and provide additional context with each message between services, Dapr uses the [CloudEvents 1.0 specification](https://github.com/cloudevents/spec/tree/v1.0) as its message format. Any message sent by an application to a topic using Dapr is automatically wrapped in a Cloud Events envelope, using [`Content-Type` header value](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/#content-types) for `datacontenttype` attribute.

For more information, read about [messaging with CloudEvents](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-cloudevents/), or [sending raw messages without CloudEvents](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-raw/).



#### Message delivery

In principle, Dapr considers a message successfully delivered once the subscriber processes the message and responds with a non-error response. For more granular control, Dapr’s pub/sub API also provides explicit statuses, defined in the response payload, with which the subscriber indicates specific handling instructions to Dapr (for example, `RETRY` or `DROP`).

#### Receiving messages with topic subscriptions

Dapr applications can subscribe to published topics via three subscription types that support the same features: declarative, streaming and programmatic.

| Subscription type | Description                                                  |
| ----------------- | ------------------------------------------------------------ |
| **Declarative**   | The subscription is defined in an **external file**. The declarative approach removes the Dapr dependency from your code and **allows for existing applications to subscribe to topics, without having to change code.** |
| **Programmatic**  | Subscription is defined in the **user code**. The programmatic approach implements the static subscription and requires an endpoint in your code. |

For more information, read [about the subscriptions in Subscription Types](https://docs.dapr.io/developing-applications/building-blocks/pubsub/subscription-methods/)



#### Declarative subscriptions

You can subscribe declaratively to a topic using an external component file. This example uses a YAML component file named `subscription.yaml`:

```yaml
apiVersion: dapr.io/v2alpha1
kind: Subscription
metadata:
  name: order
spec:
  topic: orders
  routes:
    default: /orders
  pubsubname: pubsub
scopes:
- orderprocessing
```



Here the subscription called `order`:

- Uses the pub/sub component called `pubsub` to subscribes to the topic called `orders`.
- Sets the `route` field to send all topic messages to the `/orders` endpoint in the app.
- Sets `scopes` field to scope this subscription for access only by apps with ID `orderprocessing`.



```csharp
 //Subscribe to a topic 
[HttpPost("orders")]
public void getCheckout([FromBody] int orderId)
{
    Console.WriteLine("Subscriber received : " + orderId);
}
```



The `/orders` endpoint matches the `route` defined in the subscriptions and this is where Dapr sends all topic messages to.

#### Programmatic subscriptions

The dynamic programmatic approach returns the `routes` JSON structure within the code, unlike the declarative approach’s `route` YAML structure.

```csharp
[Topic("pubsub", "orders")]
[HttpPost("/orders")]
public async Task<ActionResult<Order>>Checkout(Order order, [FromServices] DaprClient daprClient)
{
    // Logic
    return order;
}
```



or

```csharp
// Dapr subscription in [Topic] routes orders topic to this route
app.MapPost("/orders", [Topic("pubsub", "orders")] (Order order) => {
    Console.WriteLine("Subscriber received : " + order);
    return Results.Ok(order);
});
```



Both of the handlers defined above also need to be mapped to configure the `dapr/subscribe` endpoint. This is done in the application startup code while defining endpoints.

```csharp
app.UseEndpoints(endpoints =>
{
    endpoints.MapSubscribeHandler();
});
```



-----------

## Overview

Navigate to the `before/03a-dapr-challenge-3` folder 

On the third challenge, your goal is to update the state store with all the events from pizza order that we are generating from the storefront, kitchen, and delivery services. For that, you will:

- Send all the generated events to a new Dapr component, a pub/sub message broker
- Update the storefront, kitchen, and delivery services to publish a message to the pub/sub.
- Subscribe to these events in the order service, which is already managing the order state in our state store.

<img src="../../../imgs/challenge-3.png" width=50%>

To learn more about the Publish & Subscribe building block, refer to the [Dapr docs](https://docs.dapr.io/developing-applications/building-blocks/pubsub/).



## Create the Pub/Sub component

Open the `before/shared/resources` folder and create a file called `pubsub.yaml`. Add the following content:

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: pizzapubsub
spec:
  type: pubsub.redis
  version: v1
  metadata:
  - name: redisHost
    value: localhost:6379
  - name: redisPassword
    value: ""
scopes:
- pizza-storefront
- pizza-kitchen
- pizza-delivery
- pizza-order
```

Similar to the `statestore.yaml` file, this new definition creates a Dapr component called _pizzapubsub_ of type _pubsub.redis_ pointing to the local Redis instance, using Redis Streams. Each app will initialize this component to interact with it.

## Create a subscription

Still inside the `before/shared/resources` folder, create a new file called `subscription.yaml`. Add the following content to it:

```yaml
apiVersion: dapr.io/v1alpha1
kind: Subscription
metadata:
  name: pizza-subscription
spec:
  topic: orders
  route: /orders-sub
  pubsubname: pizzapubsub
scopes: 
- pizza-order
```

This file of kind `Subscription` specifies that every time the Pub/Sub `pizzapubsub` component receives a message in the `orders` topic, this message will be sent to a route called `/orders-sub` on the scoped `pizza-order` service. By setting `pizza-storefront` as the only scope, we guarantee that this subscription rule will only apply to this service and will be ignored by others. Finally, the `/orders-sub` endpoint needs to be created in the `pizza-order` service in order to receive the events.

## Install the dependencies

Navigate to root of your solution. Before you start coding, install the Dapr dependencies to the `pizza-kitchen` and the `pizza-delivery` services. The `pizza-storefront` service already has the dependencies from challenge 2.

```bash
# Navigate to the service folder and add the Dapr package
cd PizzaKitchen
dotnet add package Dapr.AspNetCore

cd ..

# Navigate to the service folder and add the Dapr package
cd PizzaDelivery
dotnet add package Dapr.AspNetCore

cd ..
```

## Register the DaprClient

Inside the `PizzaKitchen` and the `PizzaDelivery` services, open `Program.cs` and add the `DaprClient` registration to the `ServiceCollection`:

```csharp
builder.Services.AddControllers().AddDapr();
```

## Register the CloudEvents

Inside the `PizzaKitchen`, `PizzaStorefront`,  `PizzaOrder` and the `PizzaDelivery` services, open `Program.cs` and add the `UseCloudEvents` registration to the *request pipeline*:

```csharp
var app = builder.Build();

if (app.Environment.IsDevelopment())
{

}

// Dapr will send serialized event object vs. being raw CloudEvent
app.UseCloudEvents();

app.MapControllers();
app.Run();
```

## Update the Kitchen service to publish messages to the message broker

1. Inside the `PizzaKitchen` folder, navigate to `/Services/CookService.cs`. Import the DaprClient:

```csharp
using Dapr.Client;
```

2. Add a private readonly DaprClient reference:

```csharp
private readonly DaprClient _daprClient;
```

3. Add two constants to hold the names of the pub/sub component and topic you will be publishing the messages to:

```csharp
private const string PUBSUB_NAME = "pizzapubsub";
private const string TOPIC_NAME = "orders";
```

4. Update the class constructor to add the DaprClient dependency:

```csharp
public CookService(DaprClient daprClient, ILogger<CookService> logger)
{
    _daprClient = daprClient;
    _logger = logger;
}
```

5. Finally, update the `CookPizzaAsync` try-catch block with the following code:

```csharp
try
{
    foreach (var (status, duration) in stages)
    {
        order.Status = status;
        _logger.LogInformation("Order {OrderId} - {Status}", order.OrderId, status);
        
        await _daprClient.PublishEventAsync(PUBSUB_NAME, TOPIC_NAME, order);
        await Task.Delay(TimeSpan.FromSeconds(duration));
    }

    order.Status = "cooked";
    await _daprClient.PublishEventAsync(PUBSUB_NAME, TOPIC_NAME, order);
    return order;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error cooking order {OrderId}", order.OrderId);
    order.Status = "cooking_failed";
    order.Error = ex.Message;
    await _daprClient.PublishEventAsync(PUBSUB_NAME, TOPIC_NAME, order);
    return order;
}
```

The just like the previous challenges, we are using the Dapr Client to call the building block API: `await _daprClient.PublishEventAsync(PUBSUB_NAME, TOPIC_NAME, order);`.

In this case, `publishEventAsync` will publish the message `order` to the `PUBSUB_NAME` and `TOPIC_NAME` you've declared above.

Let's do the same for the Delivery and Storefront services.

## Update the Delivery service to publish messages to the message broker

1. Inside the `PizzaDelivery` folder, navigate to `/Services/DeliveryService.cs`. Import the DaprClient:

```csharp
using Dapr.Client;
```

2. Add a private readonly DaprClient reference:

```csharp
private readonly DaprClient _daprClient;
```

3. Add two constants to hold the names of the pub/sub component and topic you will be publishing the messages to:

```csharp
private const string PUBSUB_NAME = "pizzapubsub";
private const string TOPIC_NAME = "orders";
```

4. Update the class constructor to add the DaprClient dependency:

```csharp
public DeliveryService(DaprClient daprClient, ILogger<DeliveryService> logger)
{
    _daprClient = daprClient;
    _logger = logger;
}
```

5. Finally, update the `DeliverPizzaAsync` try-catch block with the following code:

```csharp
try
{
    foreach (var (status, duration) in stages)
    {
        order.Status = status;
        _logger.LogInformation("Order {OrderId} - {Status}", order.OrderId, status);
        
        await _daprClient.PublishEventAsync(PUBSUB_NAME, TOPIC_NAME, order);
        await Task.Delay(TimeSpan.FromSeconds(duration));
    }

    order.Status = "delivered";
    await _daprClient.PublishEventAsync(PUBSUB_NAME, TOPIC_NAME, order);
    return order;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error delivering order {OrderId}", order.OrderId);
    order.Status = "delivery_failed";
    order.Error = ex.Message;
    await _daprClient.PublishEventAsync(PUBSUB_NAME, TOPIC_NAME, order);
    return order;
}
```

## Update the Storefront service to publish messages to the message broker

1. Inside the `PizzaStorefront` service folder, navigate to `/Services/Storefront.cs` and add the pub/sub constants:

```csharp
private const string PUBSUB_NAME = "pizzapubsub";
private const string TOPIC_NAME = "orders";
```

2. Inside `ProcessOrderAsync` update the try-catch block with:

```csharp
try
{
    // Set pizza order status
    foreach (var (status, duration) in stages)
    {
        order.Status = status;
        _logger.LogInformation("Order {OrderId} - {Status}", order.OrderId, status);
        
        await _daprClient.PublishEventAsync(PUBSUB_NAME, TOPIC_NAME, order);
        await Task.Delay(TimeSpan.FromSeconds(duration));
    }

    _logger.LogInformation("Starting cooking process for order {OrderId}", order.OrderId);
        
    // Use the Service Invocation building block to invoke the endpoint in the pizza-kitchen service
    var response = await _daprClient.InvokeMethodAsync<Order, Order>(
        HttpMethod.Post,
        "pizza-kitchen",
        "cook",
        order);

    _logger.LogInformation("Order {OrderId} cooked with status {Status}", 
        order.OrderId, response.Status);

    // Use the Service Invocation building block to invoke the endpoint in the pizza-delivery service
    _logger.LogInformation("Starting delivery process for order {OrderId}", order.OrderId);
        
    response = await _daprClient.InvokeMethodAsync<Order, Order>(
        HttpMethod.Post,
        "pizza-delivery",
        "delivery",
        order);

    _logger.LogInformation("Order {OrderId} delivered with status {Status}", 
        order.OrderId, response.Status);

    return order;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error processing order {OrderId}", order.OrderId);
    order.Status = "failed";
    order.Error = ex.Message;
    
    await _daprClient.PublishEventAsync(PUBSUB_NAME, TOPIC_NAME, order);
    return order;
}
```

## Subscribe to events

Now that you've published the events to the topic `orders` in the message broker, you will subscribe to the same topic in the `pizza-order` service:

Navigate to the `PizzaOrder` service folder. Inside `/Controllers/OrderController.cs` find the  the route `/order-sub`:

```csharp
[HttpPost("/orders-sub")]
public async Task<IActionResult> HandleOrderUpdate(CloudEvent<Order> cloudEvent)
{
    _logger.LogInformation("Received order update for order {OrderId}", 
        cloudEvent.Data.OrderId);

    var result = await _orderStateService.UpdateOrderStateAsync(cloudEvent.Data);
    return Ok();
}
```

Following the `subscription.yaml` file spec, every time a new message lands in the `orders` topic within the `pizzapubsub` pub/sub, it will be routed to this `/orders-sub` topic. The message will then be sent to the previously created function that creates or updates the message in the state store, created in the first challenge.

```yaml
apiVersion: dapr.io/v1alpha1
kind: Subscription
metadata:
  name: pizza-subscription
spec:
  topic: orders
  route: /orders-sub
  pubsubname: pizzapubsub
scopes: 
- pizza-order
```

## Run the application

It's time to run all four applications. If the `pizza-storefront`, `pizza-kitchen`, `pizza-delivery`, and the `pizza-store` 
services are still running, press **CTRL+C** in each terminal window to stop them.

1. Open a new terminal window, navigate to the `/PizzaOrder` folder and run the command below:

```bash
dapr run --app-id pizza-order --app-protocol http --app-port 8001 --dapr-http-port 3501 --resources-path ../../shared/resources -- dotnet run
```

2. In a new terminal, navigate to the `/PizzaStorefront` folder and run the command below:

```bash
dapr run --app-id pizza-storefront --app-protocol http --app-port 8002 --dapr-http-port 3502 --resources-path ../../shared/resources -- dotnet run
```

3. Open a new terminal window and navigate to `/PizzaKitchen` folder. Run the command below:

```bash
dapr run --app-id pizza-kitchen --app-protocol http --app-port 8003 --dapr-http-port 3503 --resources-path ../../shared/resources -- dotnet run
```

4. Open a fourth terminal window and navigate to `/PizzaDelivery` folder. Run the command below:

```bash
dapr run --app-id pizza-delivery --app-protocol http --app-port 8004 --dapr-http-port 3504 --resources-path ../../shared/resources -- dotnet run
```



Check the Dapr and application logs for all four services. You should now see the pubsub component loaded in the Dapr logs:

```bash
INFO[0000] Component loaded: pizzapubsub (pubsub.redis/v1)  app_id=pizza-storefront instance=diagrid.local scope=dapr.runtime.processor 
```

## Test the service

Open `Endpoints.http` and create a new order sending the request on `Direct Pizza Store Endpoint (for testing)`, similar to what was done previous challenge.

Navigate to the `pizza-order` terminal, where you should see the following logs pop up with all the events being updated:

```bash
== APP == info: PizzaOrder.Controllers.OrderController[0]
== APP ==       Received order update for order 123
== APP == info: PizzaOrder.Services.OrderStateService[0]
== APP ==       Updated state for order 123 - Status: validating
== APP == info: PizzaOrder.Controllers.OrderController[0]
== APP ==       Received order update for order 123
== APP == info: PizzaOrder.Services.OrderStateService[0]
== APP ==       Updated state for order 123 - Status: processing
== APP == info: PizzaOrder.Controllers.OrderController[0]
== APP ==       Received order update for order 123
== APP == info: PizzaOrder.Services.OrderStateService[0]
== APP ==       Updated state for order 123 - Status: confirmed
== APP == info: PizzaOrder.Controllers.OrderController[0]
== APP ==       Received order update for order 123
== APP == info: PizzaOrder.Services.OrderStateService[0]
== APP ==       Updated state for order 123 - Status: cooking_preparing_ingredients
== APP == info: PizzaOrder.Controllers.OrderController[0]
== APP ==       Received order update for order 123
== APP == info: PizzaOrder.Services.OrderStateService[0]
== APP ==       Updated state for order 123 - Status: cooking_making_dough
== APP == info: PizzaOrder.Controllers.OrderController[0]
== APP ==       Received order update for order 123
== APP == info: PizzaOrder.Services.OrderStateService[0]
== APP ==       Updated state for order 123 - Status: cooking_adding_toppings
== APP == info: PizzaOrder.Controllers.OrderController[0]
== APP ==       Received order update for order 123
== APP == info: PizzaOrder.Services.OrderStateService[0]
== APP ==       Updated state for order 123 - Status: cooking_baking
== APP == info: PizzaOrder.Controllers.OrderController[0]
== APP ==       Received order update for order 123
== APP == info: PizzaOrder.Services.OrderStateService[0]
== APP ==       Updated state for order 123 - Status: cooking_quality_check
== APP == info: PizzaOrder.Controllers.OrderController[0]
== APP ==       Received order update for order 123
== APP == info: PizzaOrder.Services.OrderStateService[0]
== APP ==       Updated state for order 123 - Status: cooked
== APP == info: PizzaOrder.Controllers.OrderController[0]
== APP ==       Received order update for order 123
== APP == info: PizzaOrder.Services.OrderStateService[0]
== APP ==       Updated state for order 123 - Status: delivery_finding_driver
== APP == info: PizzaOrder.Controllers.OrderController[0]
== APP ==       Received order update for order 123
== APP == info: PizzaOrder.Services.OrderStateService[0]
== APP ==       Updated state for order 123 - Status: delivery_driver_assigned
== APP == info: PizzaOrder.Controllers.OrderController[0]
== APP ==       Received order update for order 123
== APP == info: PizzaOrder.Services.OrderStateService[0]
== APP ==       Updated state for order 123 - Status: delivery_picked_up
== APP == info: PizzaOrder.Controllers.OrderController[0]
== APP ==       Received order update for order 123
== APP == info: PizzaOrder.Services.OrderStateService[0]
== APP ==       Updated state for order 123 - Status: delivery_on_the_way
== APP == info: PizzaOrder.Controllers.OrderController[0]
== APP ==       Received order update for order 123
== APP == info: PizzaOrder.Services.OrderStateService[0]
== APP ==       Updated state for order 123 - Status: delivery_arriving
== APP == info: PizzaOrder.Controllers.OrderController[0]
== APP ==       Received order update for order 123
== APP == info: PizzaOrder.Services.OrderStateService[0]
== APP ==       Updated state for order 123 - Status: delivery_at_location
== APP == info: PizzaOrder.Controllers.OrderController[0]
== APP ==       Received order update for order 123
== APP == info: PizzaOrder.Services.OrderStateService[0]
== APP ==       Updated state for order 123 - Status: delivered
```


## Dapr multi-app run

Instead of opening multiple terminals to run the services, you can take advantage of a great Dapr CLI feature: [multi-app run](https://docs.dapr.io/developing-applications/local-development/multi-app-dapr-run/multi-app-overview/). This enables you run all three services with just one command!

In the parent folder, create a new file called `dapr.yaml`. Add the following content to it:

```yaml
version: 1
common:
  resourcesPath: ../shared/resources
apps:
- appDirPath: PizzaStorefront
  appID: pizza-storefront
  appPort: 8002
  command:
  - dotnet
  - run
- appDirPath: PizzaKitchen
  appID: pizza-kitchen
  appPort: 8003
  command:
  - dotnet
  - run
- appDirPath: PizzaDelivery
  appID: pizza-delivery
  appPort: 8004
  command:
  - dotnet
  - run
- appDirPath: PizzaOrder
  appID: pizza-order
  appPort: 8001
  command:
  - dotnet
  - run
```

Stop the services, if they are running, and enter the following command in the terminal:

```bash
dapr run -f .
```

All four services will run at the same time and log events at the same terminal window.



## Suggested Solution

You find a **Suggested Solution** in the `after` folder.



## Next steps

In the next challenge we will orchestrate the pizza ordering, cooking, and delivering process leberaging Dapr's Workflow API. Once you are ready, navigate to Challenge 4: [Workflows](/docs/challenge-4/dotnet.md)!

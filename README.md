# Dapr & Aspire workshop

Part one of this workshop is a clone of [Diagrid's Dapr Workshop](https://github.com/diagrid-labs/dapr-workshop)

Part two is Aspirefying the Diagrid's Dapr workshop. Here Aspire is added to the Diagrid's Dapr workshop projects.

Finally Diagrid's Dapr workshop [Workflow](https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-overview/) project is refactored to an Aspire project where all messages are send via asynchrony messaging (messages queues).



## Dapr Workshop - introduction

![Dapr sidecar](imgs/dapr_sidecar_pixelart.png)

Welcome to Diagrid's [Dapr](https://dapr.io/) Workshop! This repository contains a set of hands-on challenges designed to introduce you to Dapr's most popular APIs and give you a starting point to build your own distributed applications.

Microservices architectures are popular for a variety of reasons - they enable polyglot development, are easily scaled, and perform simple, focused tasks. However, as the number of microservices grows, so does the complexity of the system. Managing security, observability, and resiliency becomes increasingly challenging, often leading to the same problems being solved over and over again.

Dapr addresses these challenges by providing a set of APIs for building distributed systems with best practices for microservices baked in. Leveraging Dapr allows you to reduce development time while building reliable, observable, and secure distributed applications with ease. Let’s dive in and explore how Dapr can simplify your journey to distributed systems excellence!

![Dapr from dev to hosting](imgs/dapr-slidedeck-overview.png)

### Goals

On completion of this workshop, you will understand how three of the most popular Dapr Building Block APIs work: [State Management](https://docs.dapr.io/developing-applications/building-blocks/state-management/), [Service Invocation](https://docs.dapr.io/developing-applications/building-blocks/service-invocation/), [Publish/Subscribe](https://docs.dapr.io/developing-applications/building-blocks/pubsub/), and [Workflow](https://docs.dapr.io/developing-applications/building-blocks/workflow/).

You will build five microservices to simulate the process of ordering a pizza:

- The `pizza-storefront` service serves as an entry point for customers to order a new pizza.
- The `pizza-kitchen` service has a single responsibility, to cook the pizza.
- The `pizza-delivery` service manages the delivery process, from picking up the pizza at the kitchen to delivering it to the customer's doorstep.
- The `pizza-order` service manages the order status in the state store.
- The `pizza-workflow` service orchestrates the order steps, from ordering to delivery.

### Challenges

#### Challenge 1: State Management

You will start the workshop by creating the `pizza-order` service. It is responsible for managing the  order state and saving it to a Redis database, using the [Dapr State Management Building Block](https://docs.dapr.io/developing-applications/building-blocks/state-management/). You will learn how to create a Dapr Component specification, and how to use the Dapr SDK to save and retrieve an item using the State Store API.

<img src="imgs/challenge-1.png" width=50%>




#### Challenge 2: Service Invocation

This challenge will focus on synchronous communication between services using the [Dapr Service Invocation Building Block](https://docs.dapr.io/developing-applications/building-blocks/service-invocation/). After the pizza order is saved in the database, you will create the three following services: `pizza-storefront`, `pizza-kitchen`, and `pizza-delivery`. `pizza-storefront` will invoke the endpoints from the other services to cook and deliver the pizza.

<img src="imgs/challenge-2.png" width=50%>




#### Challenge 3: Pub/Sub

In the third challenge, you will add a Pub/Sub component to the `pizza-order` service. You will use the [Dapr Publish & Subscribe Building Block](https://docs.dapr.io/developing-applications/building-blocks/pubsub/) to publish events to a Redis Streams message broker from `pizza-storefront`, `pizza-kitchen`, and `pizza-delivery`. These events represent each stage in the pizza order, cooking, and delivery process. For every event published, the `pizza-order` service will subscribe to it and update the current order status in the Redis State Store.

<img src="imgs/challenge-3.png" width=60%>

Challenge 3 is in two variants - declarative pub/sub and programmatic pub/sub. In short:

- Declarative pub/sub:
  - The subscription is defined in an **external file**. The declarative approach removes the Dapr dependency from your code and **allows for existing applications to subscribe to topics, without having to change code.**
- Programmatic pub/sub:
  - Subscription is defined in the **user code**. The programmatic approach implements the static subscription and requires an endpoint in your code.



#### Challenge 4: Workflows

Now you will modify the application to orchestrate the process of ordering, cooking, and delivering the pizza to use [Dapr's Workflow Building Block](https://docs.dapr.io/developing-applications/building-blocks/workflow/). With that you will guarantee that every step happens in a particular order. A validation state will also be created to guarantee that the pizza was cooked properly before it is delivered.

<img src="imgs/workflow.png" width=75%>



### Get started

No existing knowledge of Dapr or microservices is needed to complete this workshop but basic programming skills in C# are required.
Today this workshop offers challenges in .NET. Complete the technical prerequisites and start the first challenge!

- [Prerequisites:](./docs/prerequisites.md)
- [The Pizza Store:](./docs/The Pizza Store.md)
- Challenges:
	- [Challenge 1:](./docs/dapr/challenge-1/dotnet.md)
	- [Challenge 2:](./docs/dapr/challenge-2/dotnet.md)
	- [Challenge 3a - Declarative:](./docs/dapr/challenge-3a/dotnet.md)
	- [Challenge 3b - Programmatic:](./docs/dapr/challenge-3b/dotnet.md)
	- [Challenge 4:](./docs/dapr/challenge-4/dotnet.md)



## Dapr & Aspire - introduction

[Aspire](https://aspire.dev/) is a framework for building **cloud-native applications** in .NET with a strong focus on **observability, orchestration, and developer productivity**. It complements Dapr by providing tooling and patterns to manage distributed applications more effectively.

By integrating Aspire into this workshop, you gain:

- **Unified orchestration**
   Aspire makes it easy to define and run multiple microservices together, handling dependencies and startup order automatically.
- **Enhanced observability**
   Aspire provides built-in dashboards for logs, metrics, and traces, helping you monitor the health of your pizza-ordering microservices at a glance.
- **Simplified configuration**
   Connection strings, secrets, and environment variables can be centrally managed, reducing duplication and configuration drift across services.
- **Developer productivity**
   Aspire streamlines the local development experience, so you can run your Dapr-enabled services with minimal setup and focus on solving the challenges.

#### How Aspire fits into the Pizza Workshop

- The **Aspire AppHost** can orchestrate all five pizza microservices (`storefront`, `kitchen`, `delivery`, `order`, and `workflow`) together with their Dapr sidecars.
- Aspire’s **dashboard** lets you visualize service dependencies and monitor the state of your pizza orders in real time.
- You can extend the workshop by adding Aspire components for Redis, message brokers, or other external resources, making the environment closer to production scenarios.



#### Dapr -Aspire challenges:

- [Dapr - Aspire challenge 1:](./docs/aspire/challenge-1/aspire.md)
- [Dapr - Aspire challenge 2:](./docs/aspire/challenge-2/aspire.md)
- [Dapr - Aspire challenge 3 - Programmatic:](./docs/aspire/challenge-3/aspire.md)
- [Dapr - Aspire challenge 4:](./docs/aspire/challenge-4/aspire.md)
- [Dapr - Aspire challenge 5:](./docs/aspire/challenge-5/aspire.md)
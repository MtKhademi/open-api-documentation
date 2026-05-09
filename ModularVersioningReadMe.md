## Modular OpenAPI Versioning in ASP.NET Core Minimal APIs

### Goal
In a modular system, each module should be able to:
- expose its own endpoints
- evolve independently
- have its own OpenAPI document
- version its documentation independently from other modules

For example:

- IAM module:
  - `iam-v1`
  - `iam-v2`

- Product module:
  - `products-v1`

This gives you:
- cleaner documentation
- better separation of concerns
- easier maintenance
- independent module evolution
- better client generation

---

## Why this approach is useful
In a modular monolith or modular API, not every module evolves at the same speed.

For example:
- IAM may already have `v2`
- Product may still only have `v1`

If you generate only one global OpenAPI document, the result becomes:
- crowded
- harder to navigate
- harder to version correctly
- harder for consumers to understand

A better approach is:

- one OpenAPI document per module per version
- one docs page per module per version
- endpoint routes aligned with the same versioning strategy

---

## Final target structure

### API routes

```text
/api/iam/v1/users
/api/iam/v2/users
/api/products/v1
```

### OpenAPI documents

```text
/openapi/iam-v1.json
/openapi/iam-v2.json
/openapi/products-v1.json
```

### Documentation UI routes

```text
/docs/iam-v1
/docs/iam-v2
/docs/products-v1
```

---

## High-level design
This solution has four important pieces:

1. register multiple OpenAPI documents
2. map module endpoints with versioned routes
3. attach document metadata to endpoints
4. filter each document so it only includes its own endpoints

---

## Step 1: Create the basic program setup

### `Program.cs`

```csharp
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddIamModule();
builder.Services.AddProductModule();

builder.Services.AddOpenApi("iam-v1", options =>
{
    options.AddDocumentTransformer(new ModuleInfoDocumentTransformer(
        title: "IAM API",
        version: "v1",
        description: "Identity and Access Management module - version 1"));

    options.AddDocumentTransformer(new DocumentInclusionTransformer("iam-v1"));
});

builder.Services.AddOpenApi("iam-v2", options =>
{
    options.AddDocumentTransformer(new ModuleInfoDocumentTransformer(
        title: "IAM API",
        version: "v2",
        description: "Identity and Access Management module - version 2"));

    options.AddDocumentTransformer(new DocumentInclusionTransformer("iam-v2"));
});

builder.Services.AddOpenApi("products-v1", options =>
{
    options.AddDocumentTransformer(new ModuleInfoDocumentTransformer(
        title: "Products API",
        version: "v1",
        description: "Products module - version 1"));

    options.AddDocumentTransformer(new DocumentInclusionTransformer("products-v1"));
});

var app = builder.Build();

app.MapOpenApi("/openapi/{documentName}.json");

app.MapScalarApiReference("/docs/iam-v1", options =>
{
    options.Title = "IAM API v1";
    options.OpenApiRoutePattern = "/openapi/iam-v1.json";
});

app.MapScalarApiReference("/docs/iam-v2", options =>
{
    options.Title = "IAM API v2";
    options.OpenApiRoutePattern = "/openapi/iam-v2.json";
});

app.MapScalarApiReference("/docs/products-v1", options =>
{
    options.Title = "Products API v1";
    options.OpenApiRoutePattern = "/openapi/products-v1.json";
});

app.UseIamModule();
app.UseProductModule();

app.Run();
```

---

## Step 2: Create a transformer for document metadata
This transformer is responsible only for:
- title
- version
- description

### `OpenApi/ModuleInfoDocumentTransformer.cs`

```csharp
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

internal sealed class ModuleInfoDocumentTransformer(
    string title,
    string version,
    string description) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = title,
            Version = version,
            Description = description
        };

        return Task.CompletedTask;
    }
}
```

---

## Step 3: Add custom endpoint metadata for document ownership
Each endpoint should declare which OpenAPI document it belongs to.

### `OpenApi/OpenApiDocumentNameMetadata.cs`

```csharp
public sealed record OpenApiDocumentNameMetadata(string DocumentName);
```

### `OpenApi/OpenApiDocumentExtensions.cs`

```csharp
using Microsoft.AspNetCore.Builder;

public static class OpenApiDocumentExtensions
{
    public static TBuilder WithDocumentName<TBuilder>(this TBuilder builder, string documentName)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.WithMetadata(new OpenApiDocumentNameMetadata(documentName));
        return builder;
    }
}
```

This allows you to write:

```csharp
.WithDocumentName("iam-v1")
```

on your endpoints.

---

## Step 4: Create a transformer that filters paths by document
This transformer removes paths from the generated document if they do not belong to the requested document.

### Important note
This example shows the architectural idea clearly so you can build the solution from scratch and understand the design.

Depending on your exact .NET version and available OpenAPI APIs, you may later replace this with a built-in document inclusion strategy if available.

### `OpenApi/DocumentInclusionTransformer.cs`

```csharp
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

internal sealed class DocumentInclusionTransformer(string documentName)
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var pathsToRemove = new List<string>();

        foreach (var path in document.Paths)
        {
            var keepAnyOperation = false;

            foreach (var operation in path.Value.Operations)
            {
                var endpointDescription = context.DescriptionGroups.Items
                    .SelectMany(group => group.Items)
                    .FirstOrDefault(item =>
                        string.Equals(item.RelativePath, path.Key.TrimStart('/'), StringComparison.OrdinalIgnoreCase));

                var metadata = endpointDescription?.ActionDescriptor?.EndpointMetadata
                    ?.OfType<OpenApiDocumentNameMetadata>()
                    .FirstOrDefault();

                if (metadata?.DocumentName == documentName)
                {
                    keepAnyOperation = true;
                    break;
                }
            }

            if (!keepAnyOperation)
            {
                pathsToRemove.Add(path.Key);
            }
        }

        foreach (var path in pathsToRemove)
        {
            document.Paths.Remove(path);
        }

        return Task.CompletedTask;
    }
}
```

---

## Step 5: Create the IAM module

### `Modules/IamModule/IamModule.cs`

```csharp
using Microsoft.AspNetCore.Http;

public static class IamModule
{
    public static IServiceCollection AddIamModule(this IServiceCollection services)
    {
        return services;
    }

    public static WebApplication UseIamModule(this WebApplication app)
    {
        var iamV1 = app.MapGroup("/api/iam/v1")
            .WithTags("IAM")
            .WithOpenApi();

        iamV1.MapGet("/users", () =>
        {
            return TypedResults.Ok(new[]
            {
                new IamUserV1Response(1, "ali"),
                new IamUserV1Response(2, "sara")
            });
        })
        .WithName("GetIamUsersV1")
        .WithSummary("Get IAM users v1")
        .WithDescription("Returns IAM users for version 1.")
        .Produces<IamUserV1Response[]>(StatusCodes.Status200OK)
        .WithDocumentName("iam-v1");

        var iamV2 = app.MapGroup("/api/iam/v2")
            .WithTags("IAM")
            .WithOpenApi();

        iamV2.MapGet("/users", () =>
        {
            return TypedResults.Ok(new[]
            {
                new IamUserV2Response(1, "ali", "ali@test.com"),
                new IamUserV2Response(2, "sara", "sara@test.com")
            });
        })
        .WithName("GetIamUsersV2")
        .WithSummary("Get IAM users v2")
        .WithDescription("Returns IAM users for version 2 with email.")
        .Produces<IamUserV2Response[]>(StatusCodes.Status200OK)
        .WithDocumentName("iam-v2");

        return app;
    }
}

public sealed record IamUserV1Response(int Id, string UserName);
public sealed record IamUserV2Response(int Id, string UserName, string Email);
```

---

## Step 6: Create the Product module

### `Modules/ProductModule/ProductModule.cs`

```csharp
using Microsoft.AspNetCore.Http;

public static class ProductModule
{
    public static IServiceCollection AddProductModule(this IServiceCollection services)
    {
        return services;
    }

    public static WebApplication UseProductModule(this WebApplication app)
    {
        var productsV1 = app.MapGroup("/api/products/v1")
            .WithTags("Products")
            .WithOpenApi();

        productsV1.MapGet("/", () =>
        {
            return TypedResults.Ok(new[]
            {
                new ProductResponse(1, "Keyboard", 120),
                new ProductResponse(2, "Mouse", 80)
            });
        })
        .WithName("GetProductsV1")
        .WithSummary("Get products v1")
        .WithDescription("Returns all products for version 1.")
        .Produces<ProductResponse[]>(StatusCodes.Status200OK)
        .WithDocumentName("products-v1");

        return app;
    }
}

public sealed record ProductResponse(int Id, string Name, decimal Price);
```

---

## How it works end to end

### IAM v1
- route:
  ```text
  /api/iam/v1/users
  ```
- document:
  ```text
  iam-v1
  ```
- OpenAPI output:
  ```text
  /openapi/iam-v1.json
  ```
- docs page:
  ```text
  /docs/iam-v1
  ```

### IAM v2
- route:
  ```text
  /api/iam/v2/users
  ```
- document:
  ```text
  iam-v2
  ```
- OpenAPI output:
  ```text
  /openapi/iam-v2.json
  ```
- docs page:
  ```text
  /docs/iam-v2
  ```

### Products v1
- route:
  ```text
  /api/products/v1
  ```
- document:
  ```text
  products-v1
  ```
- OpenAPI output:
  ```text
  /openapi/products-v1.json
  ```
- docs page:
  ```text
  /docs/products-v1
  ```

---

## Suggested folder structure

```text
/OpenApi
  ModuleInfoDocumentTransformer.cs
  DocumentInclusionTransformer.cs
  OpenApiDocumentNameMetadata.cs
  OpenApiDocumentExtensions.cs

/Modules
  /IamModule
    IamModule.cs
  /ProductModule
    ProductModule.cs

Program.cs
```

---

## Why this design is good

### 1) Each module evolves independently
IAM can move to `v2` while Product remains at `v1`.

### 2) Each document stays focused
Consumers who need IAM documentation do not need to see Product endpoints.

### 3) Better client generation
A frontend or external team can generate a client only for:
- IAM v1
- IAM v2
- Product v1

### 4) Cleaner release management
Versioning becomes module-specific instead of application-wide.

### 5) Better scalability
As the application grows, documentation stays modular.

---

## Best practices

- keep one OpenAPI document per module per version
- align route versioning with documentation versioning
- keep document info and endpoint filtering separate
- attach document ownership using metadata
- keep module endpoint registration inside the module
- keep `Program.cs` focused on orchestration, not business details
- use dedicated response DTOs per version when contracts differ

---

## Common mistakes

### Mistake 1: one giant OpenAPI document for everything
Problem:
- docs become crowded
- versioning becomes confusing
- client generation becomes harder

### Mistake 2: versioning only in route, not in docs
Problem:
- consumers cannot easily discover module/version boundaries

### Mistake 3: forcing all modules to share the same version
Problem:
- modules become unnecessarily coupled
- release flexibility is reduced

### Mistake 4: putting all OpenAPI logic in `Program.cs`
Problem:
- poor maintainability
- poor readability
- harder scaling

---

## If you want to make it even better later
After you fully understand this basic modular versioning model, you can improve it with:
- authentication-aware docs per module
- separate security schemes per module
- version deprecation notes
- module-specific tags and contact metadata
- configuration-based document registration
- custom operation filtering strategies

But do not start there.

First, make this simpler version work end to end.

---

## Final mental model
Think of the architecture like this:

- one module can have many versions
- each version can have its own endpoints
- each module-version pair can have its own OpenAPI document
- each OpenAPI document should only include the endpoints that belong to it

That gives you a clean and scalable modular documentation strategy.

---

## Final recommendation
If you are building a modular ASP.NET Core application, prefer this model:

- module-per-document
- version-per-document
- versioned routes
- document ownership via metadata
- focused docs UI per module and version

This is one of the cleanest ways to keep modular APIs understandable as the system grows.
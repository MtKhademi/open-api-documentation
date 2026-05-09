## ASP.NET Core OpenAPI Document Transformers: From Basics to Advanced

### What is an OpenAPI Document Transformer?
An OpenAPI Document Transformer is a customization hook that lets you modify the generated OpenAPI document before it is returned by the framework.

In simple words:

- ASP.NET Core generates the OpenAPI document automatically from your endpoints.
- A document transformer lets you change that generated document before it is exposed through `/openapi`, Swagger, Scalar, or any other OpenAPI consumer.

You can use a transformer to:
- change the API title, version, or description
- add contact or license information
- add server URLs
- add security schemes such as Bearer authentication
- hide specific endpoints from the document
- normalize `operationId` values
- enrich missing descriptions
- shape the document based on configuration, environment, tenant, role, or feature flags

So the mental model is:

- OpenAPI generator creates the document
- Transformer modifies the document
- Final transformed document is returned to the client

---

### Why do transformers exist?
Because the framework can generate a lot of OpenAPI information automatically, but not everything can or should be inferred.

There are many cases where you want to customize the generated output:
- documentation metadata is incomplete
- security settings should appear in the docs
- some endpoints should be hidden
- the document should change based on environment or configuration
- documentation should match the real runtime behavior more closely

Transformers solve this problem.

---

### Where are transformers registered?
They are registered inside `AddOpenApi(...)` using `AddDocumentTransformer(...)`.

Example:

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "Checkout API",
            Version = "v1",
            Description = "API for processing checkouts."
        };

        return Task.CompletedTask;
    });
});
```

---

### Three ways to register a document transformer

#### 1) Register using a delegate
Use this when the customization is small and simple.

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "Checkout API",
            Version = "v1"
        };

        return Task.CompletedTask;
    });
});
```

Best for:
- quick customization
- small projects
- simple metadata changes

---

#### 2) Register using an instance of `IOpenApiDocumentTransformer`
Use this when you want a separate class but do not need dependency injection.

```csharp
internal sealed class MyDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new()
        {
            Title = "Checkout API",
            Version = "v1"
        };

        return Task.CompletedTask;
    }
}
```

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer(new MyDocumentTransformer());
});
```

Best for:
- reusable logic
- keeping `Program.cs` cleaner
- simple classes without DI dependencies

---

#### 3) Register using a DI-activated `IOpenApiDocumentTransformer`
Use this when your transformer needs services from dependency injection such as:
- `IConfiguration`
- `IHostEnvironment`
- `IAuthenticationSchemeProvider`
- `ILogger<T>`
- custom services

```csharp
internal sealed class OpenApiInfoTransformer(IConfiguration configuration) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new()
        {
            Title = configuration["OpenApi:Title"],
            Version = configuration["OpenApi:Version"]
        };

        return Task.CompletedTask;
    }
}
```

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<OpenApiInfoTransformer>();
});
```

Best for:
- production applications
- scalable design
- clean architecture
- dynamic behavior based on services or config

---

### Can multiple transformers be registered?
Yes. You can register multiple transformers, and they execute in the same order in which they are registered.

Example:

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "Checkout API",
            Version = "v1"
        };

        return Task.CompletedTask;
    });

    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Description = "API for processing customer checkouts.";
        return Task.CompletedTask;
    });
});
```

Execution order:
1. first transformer runs
2. second transformer runs

Important rule:
If multiple transformers modify the same property, the last one wins.

---

### Simple examples: from easiest to more useful

### Example 1: Set title, version, and description
This is the simplest and most common transformer usage.

Use case:
- improve documentation metadata

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "Checkout API",
            Version = "v1",
            Description = "API for processing checkout requests."
        };

        return Task.CompletedTask;
    });
});
```

---

### Example 2: Add contact and license info
Use case:
- professional API documentation
- public or team-facing APIs

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "Checkout API",
            Version = "v1",
            Description = "API for processing checkout requests.",
            Contact = new OpenApiContact
            {
                Name = "API Support Team",
                Email = "support@myapp.com"
            },
            License = new OpenApiLicense
            {
                Name = "MIT"
            }
        };

        return Task.CompletedTask;
    });
});
```

---

### Example 3: Add server URLs
Use case:
- tell API consumers which base URLs are valid
- support local, staging, and production environments

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Servers = new List<OpenApiServer>
        {
            new() { Url = "https://localhost:5001", Description = "Local Development" },
            new() { Url = "https://staging-api.myapp.com", Description = "Staging" },
            new() { Url = "https://api.myapp.com", Description = "Production" }
        };

        return Task.CompletedTask;
    });
});
```

---

### Example 4: Add a JWT Bearer security scheme
Use case:
- your API uses bearer token authentication
- you want the OpenAPI document to describe that security model

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "JWT",
                Description = "JWT Authorization header using the Bearer scheme."
            }
        };

        return Task.CompletedTask;
    });
});
```

Important:
This only defines the security scheme.  
It does not automatically apply the scheme to every operation.

---

### Example 5: Read OpenAPI metadata from configuration
Use case:
- avoid hardcoding metadata
- change docs through configuration

```csharp
internal sealed class ConfigurationBasedInfoTransformer(IConfiguration configuration)
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new()
        {
            Title = configuration["OpenApi:Title"],
            Version = configuration["OpenApi:Version"],
            Description = configuration["OpenApi:Description"]
        };

        return Task.CompletedTask;
    }
}
```

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<ConfigurationBasedInfoTransformer>();
});
```

---

### Example 6: Add multiple transformers with separate responsibilities
Use case:
- keep code clean
- follow single responsibility
- separate metadata, security, and other concerns

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<ConfigurationBasedInfoTransformer>();
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});
```

This is a clean design because:
- one transformer handles document info
- another handles security
- responsibilities stay separate

---

## Advanced examples: real-world scenarios

### Example 7: Feature flag based path removal
Use case:
- an endpoint exists in code, but the feature is disabled
- you do not want it visible in the documentation

```csharp
internal sealed class FeatureFlagPathRemovalTransformer(IConfiguration configuration)
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var isNewCheckoutEnabled = configuration.GetValue<bool>("Features:NewCheckout");

        if (!isNewCheckoutEnabled)
        {
            document.Paths.Remove("/api/new-checkout");
        }

        return Task.CompletedTask;
    }
}
```

Why this is useful:
- beta features
- staged rollout
- dark launch features

---

### Example 8: Environment-aware documentation
Use case:
- the document should look different in development and production

```csharp
internal sealed class EnvironmentAwareDocsTransformer(IHostEnvironment environment)
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info.Description = environment.IsDevelopment()
            ? "Development API documentation"
            : "Production API documentation";

        document.Servers = environment.IsDevelopment()
            ? new List<OpenApiServer>
            {
                new() { Url = "https://localhost:5001", Description = "Local Development" },
                new() { Url = "https://staging-api.myapp.com", Description = "Staging" }
            }
            : new List<OpenApiServer>
            {
                new() { Url = "https://api.myapp.com", Description = "Production" }
            };

        return Task.CompletedTask;
    }
}
```

Why this is useful:
- clearer documentation per environment
- fewer mistakes by API consumers

---

### Example 9: Inject security requirements into protected operations
Use case:
- some endpoints require authentication
- the OpenAPI document should make that explicit

```csharp
internal sealed class SecurityRequirementInjectionTransformer
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        foreach (var path in document.Paths)
        {
            if (!path.Key.StartsWith("/api/private", StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var operation in path.Value.Operations)
            {
                operation.Value.Security ??= new List<OpenApiSecurityRequirement>();

                operation.Value.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    }] = Array.Empty<string>()
                });
            }
        }

        return Task.CompletedTask;
    }
}
```

Why this is useful:
- security scheme exists at document level
- protected operations also explicitly show auth requirement

---

### Example 10: Normalize `OperationId`
Use case:
- generated clients should have consistent method names
- operation naming should be predictable

```csharp
internal sealed class OperationIdNormalizationTransformer
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        foreach (var path in document.Paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                var method = operation.Key.ToString().ToLowerInvariant();
                var normalizedPath = path.Key
                    .Replace("/", "_")
                    .Replace("{", "")
                    .Replace("}", "")
                    .Trim('_');

                operation.Value.OperationId = $"{method}_{normalizedPath}";
            }
        }

        return Task.CompletedTask;
    }
}
```

Example result:
- `GET /api/products/{id}` -> `get_api_products_id`

Why this is useful:
- client generation
- naming consistency
- less confusion in generated SDKs

---

### Example 11: Add default summaries and descriptions
Use case:
- some endpoints are missing documentation
- you want a fallback description

```csharp
internal sealed class DefaultDescriptionEnrichmentTransformer
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        foreach (var path in document.Paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                operation.Value.Summary ??= $"Handles {operation.Key} {path.Key}";
                operation.Value.Description ??= $"Auto-generated description for {operation.Key} {path.Key}.";
            }
        }

        return Task.CompletedTask;
    }
}
```

Why this is useful:
- avoids empty docs
- improves baseline documentation quality

---

### Example 12: Hide internal or admin endpoints
Use case:
- some endpoints are not meant for public API consumers

```csharp
internal sealed class InternalAdminPathHidingTransformer
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var hiddenPaths = document.Paths
            .Where(p => p.Key.StartsWith("/internal", StringComparison.OrdinalIgnoreCase) ||
                        p.Key.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Key)
            .ToList();

        foreach (var path in hiddenPaths)
        {
            document.Paths.Remove(path);
        }

        return Task.CompletedTask;
    }
}
```

Why this is useful:
- public docs stay clean
- internal/admin operations stay hidden

---

### Example 13: Tenant-aware documentation
Use case:
- different tenants or pricing plans have different API capabilities

```csharp
internal sealed class TenantAwareDocsTransformer(IHttpContextAccessor httpContextAccessor)
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var tenant = httpContextAccessor.HttpContext?.Request.Headers["X-Tenant"].ToString();

        if (string.Equals(tenant, "basic", StringComparison.OrdinalIgnoreCase))
        {
            document.Paths.Remove("/api/premium-reports");
            document.Paths.Remove("/api/advanced-analytics");
        }

        return Task.CompletedTask;
    }
}
```

Why this is useful:
- SaaS systems
- plan-based documentation
- different docs for Basic / Pro / Enterprise plans

---

### Example 14: Role-aware documentation
Use case:
- admins should see admin APIs
- non-admin users should not

```csharp
internal sealed class RoleAwareDocsTransformer(IHttpContextAccessor httpContextAccessor)
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var isAdmin = user?.IsInRole("Admin") == true;

        if (!isAdmin)
        {
            document.Paths.Remove("/api/admin/users");
            document.Paths.Remove("/api/admin/audit-logs");
        }

        return Task.CompletedTask;
    }
}
```

Why this is useful:
- internal admin portals
- role-based API docs
- operator-facing systems

---

### Example 15: Policy-aware tagging
Use case:
- endpoints related to a specific policy or business process should be tagged

```csharp
internal sealed class PolicyAwareTaggingTransformer
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        foreach (var path in document.Paths)
        {
            if (!path.Key.Contains("payments", StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var operation in path.Value.Operations)
            {
                operation.Value.Tags ??= new List<OpenApiTag>();
                operation.Value.Tags.Add(new OpenApiTag { Name = "RequiresApproval" });
            }
        }

        return Task.CompletedTask;
    }
}
```

Why this is useful:
- payment systems
- approval workflows
- policy-driven documentation grouping

---

### Example 16: Version-aware shaping
Use case:
- different API versions expose different endpoints

```csharp
internal sealed class VersionAwareShapingTransformer(IConfiguration configuration)
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var version = configuration["OpenApi:Version"] ?? "v1";

        document.Info.Version = version;

        if (version == "v1")
        {
            document.Paths.Remove("/api/v2/checkout/preview");
            document.Paths.Remove("/api/v2/orders/bulk");
        }

        return Task.CompletedTask;
    }
}
```

Why this is useful:
- version-specific documentation
- migration periods between v1 and v2
- reducing confusion for API consumers

---

### Example 17: Normalize response descriptions
Use case:
- response descriptions are missing or inconsistent
- you want standard descriptions for common status codes

```csharp
internal sealed class ResponseDescriptionNormalizationTransformer
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        foreach (var path in document.Paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                foreach (var response in operation.Value.Responses)
                {
                    if (!string.IsNullOrWhiteSpace(response.Value.Description))
                        continue;

                    response.Value.Description = response.Key switch
                    {
                        "200" => "Request completed successfully.",
                        "201" => "Resource created successfully.",
                        "400" => "The request is invalid.",
                        "401" => "Authentication is required.",
                        "403" => "Access is forbidden.",
                        "404" => "Resource was not found.",
                        "500" => "An unexpected server error occurred.",
                        _ => "Response"
                    };
                }
            }
        }

        return Task.CompletedTask;
    }
}
```

Why this is useful:
- consistent docs
- more readable responses
- better generated clients and consumer experience

---

### Example 18: Multi-transformer advanced composition
Use case:
- one transformer should not do everything
- each transformer should have one responsibility

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<EnvironmentAwareDocsTransformer>();
    options.AddDocumentTransformer<SecurityRequirementInjectionTransformer>();
    options.AddDocumentTransformer<InternalAdminPathHidingTransformer>();
    options.AddDocumentTransformer<OperationIdNormalizationTransformer>();
    options.AddDocumentTransformer<ResponseDescriptionNormalizationTransformer>();
});
```

Why this is useful:
- clean architecture
- easier testing
- easier maintenance
- easier debugging
- follows single responsibility principle better

Important:
Execution order is the same as registration order.

If multiple transformers change the same part of the document, the last one wins.

---

### Recommended learning path
If you are learning transformers seriously, follow this order:

1. understand what a transformer is
2. start with inline delegate transformers
3. customize `Info`
4. add server URLs
5. add security schemes
6. move logic into dedicated classes
7. use DI-activated transformers
8. split logic into multiple transformers
9. apply advanced shaping such as:
   - feature flag removal
   - environment-aware docs
   - security requirement injection
   - operation normalization
   - hidden internal paths
   - version-aware shaping

---

### Best practices
- keep each transformer focused on one responsibility
- avoid one huge transformer class that does everything
- prefer DI-activated transformers for scalable production code
- keep OpenAPI configuration aligned with real application behavior
- do not overcomplicate documentation unless there is real business value
- use multiple transformers when responsibilities are clearly separate
- be careful with execution order when multiple transformers modify the same part of the document

---

### Common mistakes
- putting all logic in a giant transformer
- hardcoding values that should come from configuration
- defining a security scheme but forgetting security requirements on operations
- making documentation too dynamic and unpredictable
- registering many transformers that overwrite each other without intention
- forgetting that last write wins when multiple transformers modify the same property

---

### Final summary
An OpenAPI Document Transformer is one of the most powerful customization points in ASP.NET Core OpenAPI support.

Use it when you want to go beyond the default generated document and make your documentation:
- more accurate
- more secure
- more professional
- more maintainable
- more aligned with real application behavior

Start simple:
- title
- version
- description

Then move to practical improvements:
- servers
- security
- configuration-based metadata

Then use advanced patterns when needed:
- feature flags
- environment-aware docs
- role-aware docs
- tenant-aware docs
- operation normalization
- response enrichment
- multi-transformer composition

The best design is usually not the most complex one.
The best design is the one that keeps documentation correct, maintainable, and aligned with the real behavior of the API.
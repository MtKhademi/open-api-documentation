## Senior-Level ASP.NET Core Minimal API Guide: `Produces`, `Results`, `TypedResults`, and `ProblemDetails`

### Overview
This section summarizes the most important concepts discussed about ASP.NET Core Minimal APIs, especially around response contracts, Swagger/OpenAPI metadata, strongly-typed results, and standardized error handling.


---

### 1) Why response metadata matters in Minimal APIs
In Minimal APIs, an endpoint should not only work correctly, it should also clearly describe:
- which HTTP status codes it may return
- what response body each status code has
- how clients should understand success and failure responses

This matters because:
- Swagger/OpenAPI becomes accurate
- frontend/backend consumers understand the contract
- generated clients behave better
- the API becomes more maintainable
- response behavior becomes explicit instead of implicit

---

### 2) What `.Produces(...)` does
The following methods:
- `.Produces<T>(statusCode)`
- `.Produces(statusCode)`
- `.ProducesProblem(statusCode)`
- `.ProducesValidationProblem(statusCode)`

are used to document the response contract of an endpoint.

They do not generate the response automatically.

Example:

```csharp
app.MapGet("/api/products", (ProductService service) =>
{
    var products = service.GetAll();
    return TypedResults.Ok(products);
})
.Produces<List<Product>>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status500InternalServerError);
```

Meaning:
- if the request succeeds, the endpoint returns `200 OK` with `List<Product>`
- if an internal server error occurs, the endpoint may return a problem response

Important:
These methods are mainly for:
- Swagger / OpenAPI documentation
- endpoint discoverability
- API consumer clarity

They do not replace the actual `return` statement in the handler.

---

### 3) Difference between the `Produces` methods

#### `.Produces<T>(statusCode)`
Use when the response body has a known type.

Example:

```csharp
.Produces<Product>(StatusCodes.Status200OK)
```

This says:
- status code is `200`
- response body is `Product`

#### `.Produces(statusCode)`
Use when the response has no body.

Example:

```csharp
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound)
```

Use this for cases like:
- `204 NoContent`
- plain `404 NotFound`
- plain `400 BadRequest`

#### `.ProducesProblem(statusCode)`
Use when the error response is expected to be a `ProblemDetails` response.

Example:

```csharp
.ProducesProblem(StatusCodes.Status500InternalServerError)
```

This is useful when your API returns standardized problem responses for server errors.

#### `.ProducesValidationProblem(statusCode)`
Use when validation errors return a `ValidationProblemDetails` response.

Example:

```csharp
.ProducesValidationProblem(StatusCodes.Status400BadRequest)
```

Wrong usage example:

```csharp
.ProducesValidationProblem(StatusCodes.Status204NoContent)
```

This is incorrect because:
- `ValidationProblem` means request validation failed
- `204 NoContent` means request succeeded with no body

These meanings conflict with each other.

---

### 4) Good vs bad design for collection endpoints
For endpoints such as `GET /api/products`, the better practice is usually:
- `200 OK` with `[]` when there is no data

instead of:
- `204 NoContent`

Why `200 OK` with empty array is usually better:
- it gives clients a stable response shape
- frontend and API clients can always expect a JSON array
- clients do not need two different success-handling paths

Recommended example:

```csharp
app.MapGet("/api/products", (ProductService service) =>
{
    var products = service.GetAll();
    return TypedResults.Ok(products);
})
.Produces<List<Product>>(StatusCodes.Status200OK);
```

---

### 5) What `Results` is
`Results` is a static factory class in Minimal APIs used to create HTTP result objects.

Examples:

```csharp
Results.Ok(product)
Results.NotFound()
Results.BadRequest()
Results.Created("/api/products/10", product)
```

These methods return `IResult`.

That means when using `Results`, different branches are usually unified under one common return type:

```csharp
IResult
```

Example:

```csharp
app.MapGet("/api/products/{id:int}", (int id, ProductService service) =>
{
    var product = service.FindById(id);

    if (product is null)
        return Results.NotFound();

    return Results.Ok(product);
});
```

Strength of `Results`:
- easy to write
- simple for small endpoints
- good for prototypes

Weakness of `Results`:
- less explicit
- weaker API contract
- less compile-time guidance
- less expressive for larger APIs

---

### 6) What `TypedResults` is
`TypedResults` is the strongly-typed version of `Results`.

Examples:

```csharp
TypedResults.Ok(product)
TypedResults.NotFound()
TypedResults.BadRequest()
TypedResults.Created("/api/products/10", product)
TypedResults.Problem(...)
```

Unlike `Results`, `TypedResults` returns specific concrete result types such as:
- `Ok<T>`
- `NotFound`
- `BadRequest`
- `Created<T>`
- `ProblemHttpResult`
- `ValidationProblem`

Why `TypedResults` is important:
- stronger contracts
- clearer code
- better maintainability
- better compile-time safety
- better compatibility with typed endpoint return declarations

---

### 7) `Results` vs `TypedResults`

| Topic | `Results` | `TypedResults` |
|---|---|---|
| Return style | generic | strongly typed |
| Common return type | `IResult` | concrete result types |
| Simplicity | easier for beginners | better for production |
| Compile-time safety | lower | higher |
| API contract clarity | lower | higher |
| Best use case | small/simple endpoints | maintainable production APIs |

Short rule:
- Use `Results` for quick/simple endpoints
- Use `TypedResults` for serious API design

---

### 8) The most important confusion: `Results` has two meanings
In Minimal APIs, the word `Results` may refer to two different things.

#### A) Factory class
Example:

```csharp
Results.Ok(product)
Results.NotFound()
```

This is the helper class that creates `IResult` objects.

#### B) Generic union return type
Example:

```csharp
Results<Ok<Product>, NotFound>
```

This is a typed union that says:
this endpoint may only return one of these result types.

This is very important when using `TypedResults`.

---

### 9) Why `TypedResults` may cause compile errors
Consider this endpoint:

```csharp
app.MapGet("/api/products/{id:int}", (int id, ProductService service) =>
{
    if (id <= 0)
        return TypedResults.BadRequest();

    var entity = service.FindById(id);
    if (entity == null)
        return TypedResults.NotFound();

    return TypedResults.Ok(entity);
});
```

This may fail to compile because each branch returns a different concrete type:
- `BadRequest`
- `NotFound`
- `Ok<Product>`

The compiler cannot always infer one valid unified return type automatically.

This can lead to misleading errors such as:

```text
Delegate 'RequestDelegate' does not take 2 arguments
```

Real cause:
The real problem is not your lambda argument count.  
The real problem is failed overload resolution caused by ambiguous result typing.

---

### 10) Correct solution for multiple typed results
When using multiple `TypedResults`, explicitly declare the endpoint return type.

Correct example:

```csharp
app.MapGet("/api/products/{id:int}",
    Results<BadRequest, NotFound, Ok<Product>> (int id, ProductService service) =>
    {
        if (id <= 0)
            return TypedResults.BadRequest();

        var entity = service.FindById(id);
        if (entity == null)
            return TypedResults.NotFound();

        return TypedResults.Ok(entity);
    });
```

Why this works:
Because now the compiler knows the endpoint may return only:
- `BadRequest`
- `NotFound`
- `Ok<Product>`

This is the preferred strongly-typed Minimal API style.

---

### 11) Best practice for production Minimal APIs
For production-grade ASP.NET Core Minimal APIs, the preferred style is:
- use `TypedResults`
- declare union return types with `Results<T1, T2, ...>`
- use `.Produces(...)` to document the contract
- use `ProblemDetails` for structured errors

Recommended mindset:
- `Results` = quick and generic
- `TypedResults` = explicit and maintainable
- `Results<T1, T2, ...>` = official contract of possible responses

---

### 12) What `ProblemDetails` is
`ProblemDetails` is a standard structure for HTTP error responses.

Typical shape:

```json
{
  "type": "https://example.com/problems/invalid-id",
  "title": "Invalid id",
  "status": 400,
  "detail": "Product id must be greater than zero.",
  "instance": "/api/products/0"
}
```

Common properties:
- `type`
- `title`
- `status`
- `detail`
- `instance`

Purpose:
- standardize error responses
- make errors easier for clients to understand
- support both human-readable and machine-readable error handling

---

### 13) What the `type` property means in `ProblemDetails`
The `type` property is a URI that identifies the category of problem.

It is not the .NET class name or exception type.

Correct mental model:
- `status` = HTTP error status
- `title` = short human-readable summary
- `detail` = detailed human-readable explanation
- `type` = stable machine-readable identifier for the kind of error

Example:

```json
{
  "type": "https://api.myapp.com/problems/product-not-found",
  "title": "Product not found",
  "status": 404,
  "detail": "No product found with id 15."
}
```

Why `type` is useful:
- lets clients distinguish different errors with the same status code
- gives a stable identifier for error handling
- avoids depending on fragile text messages

Example:
Two different `400 BadRequest` responses can have different `type` values:
- `https://api.myapp.com/problems/invalid-id`
- `https://api.myapp.com/problems/invalid-price-range`

This helps the client know exactly what went wrong.

---

### 14) Does `type` need to be a real URL?
Not strictly required, but ideally yes.

Best practice:
Make it a real documentation URL if possible.

Example:

```csharp
return TypedResults.Problem(
    type: "https://api.myapp.com/problems/invalid-product-id",
    title: "Invalid product id",
    detail: "Product id must be greater than zero.",
    statusCode: StatusCodes.Status400BadRequest);
```

---

### 15) `BadRequest()` / `NotFound()` vs `Problem(...)`

#### `TypedResults.BadRequest()` / `TypedResults.NotFound()`
These usually return only the status code response.

Example:

```csharp
return TypedResults.BadRequest();
return TypedResults.NotFound();
```

#### `TypedResults.Problem(...)`
This returns a structured `ProblemDetails` response.

Example:

```csharp
return TypedResults.Problem(
    title: "Product not found",
    detail: $"No product found with id {id}.",
    statusCode: StatusCodes.Status404NotFound);
```

Use `Problem(...)` when you want:
- standard structured errors
- richer client error information
- better API consistency

---

### 16) Matching `.Produces(...)` with actual return type
If the endpoint returns:

```csharp
return TypedResults.BadRequest();
return TypedResults.NotFound();
```

then this is more accurate:

```csharp
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound)
```

If the endpoint returns:

```csharp
return TypedResults.Problem(...);
```

then this is accurate:

```csharp
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
```

Important:
Your Swagger metadata should match the real behavior of the endpoint.

---

### 17) Wrong vs correct examples

#### Wrong
- declaring `ProducesValidationProblem(204)`
- returning plain `BadRequest()` but documenting `ProblemDetails`
- using multiple `TypedResults` without explicit `Results<T1, T2, ...>`
- using `204 NoContent` for list endpoints when `200 []` is more stable

#### Correct
- use `.ProducesValidationProblem(StatusCodes.Status400BadRequest)` for validation errors
- use `.Produces<ProblemDetails>(...)` only when actually returning problem payloads
- use `Results<BadRequest, NotFound, Ok<Product>>` with multiple typed results
- use `200 OK` with empty array for collection endpoints

---

### 18) Production-style recommended example

```csharp
app.MapGet("/api/products/{id:int}",
    Results<ProblemHttpResult, Ok<Product>> (int id, ProductService service) =>
    {
        if (id <= 0)
        {
            return TypedResults.Problem(
                type: "https://api.myapp.com/problems/invalid-product-id",
                title: "Invalid product id",
                detail: "Product id must be greater than zero.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var entity = service.FindById(id);

        if (entity == null)
        {
            return TypedResults.Problem(
                type: "https://api.myapp.com/problems/product-not-found",
                title: "Product not found",
                detail: $"No product found with id {id}.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return TypedResults.Ok(entity);
    })
    .WithName("GetProductById")
    .WithTags("Products")
    .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
    .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
    .Produces<Product>(StatusCodes.Status200OK);
```

Why this version is strong:
- explicit response contract
- structured and standard errors
- Swagger stays correct
- API clients get predictable behavior
- maintainable in real-world projects

---

### 19) Final senior-level summary
- `.Produces(...)` documents endpoint responses for OpenAPI/Swagger
- `Results` is the simple factory that returns `IResult`
- `TypedResults` is the strongly-typed factory that returns concrete result objects
- `Results<T1, T2, ...>` defines the allowed set of typed results for an endpoint
- `ProblemDetails` is the standard model for HTTP errors
- `ProblemDetails.type` is a stable URI that identifies the error category
- for production APIs, prefer:
  - `TypedResults`
  - explicit union result types
  - accurate `.Produces(...)`
  - `ProblemDetails` for standardized errors

### 20) Senior recommendation
If you are building a serious ASP.NET Core Minimal API:
- do not treat response design as a side detail
- make success and error contracts explicit
- keep Swagger aligned with real runtime behavior
- use typed results for maintainability
- use standardized problem responses for long-term API quality
using Microsoft.AspNetCore.Mvc;

namespace Modules.ProductModule;

public static class ProductModule
{
    public const string DocumentName = "products-v1";

    public static IServiceCollection AddProductModule(this IServiceCollection services)
    {
        services.AddScoped<ProductService>();

        services.AddOpenApi(DocumentName, options =>
        {
            options.AddDocumentTransformer(new ModuleInfoDocumentTransformer(
                title: "Products API",
                version: "v1",
                description: "Products module - version 1"));

            options.AddDocumentTransformer(new DocumentInclusionTransformer(DocumentName));
        });

        return services;
    }

    public static WebApplication UseProductModule(this WebApplication app)
    {
        app.MapGet("/api/products", ([FromServices] ProductService service) =>
        {
            return Results.Ok(service.GetAll());
        })
        .WithSummary("Get all products")
        .WithTags("Products")
        .Produces<List<Product>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithDocumentName(DocumentName);

        app.MapGet("/api/products/{id:int}", (int id, [FromServices] ProductService service) =>
        {
            if (id <= 0)
            {
                var validationProblem = new HttpValidationProblemDetails();
                validationProblem.Errors.Add(nameof(id),
                [
                    $"{nameof(id)} can not be zero.",
                    $"{nameof(id)} can not be negative."
                ]);

                return Results.BadRequest(validationProblem);
            }

            var entity = service.FindById(id);
            if (entity is null)
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Product not found",
                    Detail = $"No product found with id: {id}",
                    Type = "/api/products/id/not-found"
                };

                return Results.NotFound(problemDetails);
            }

            return Results.Ok(entity);
        })
        .WithSummary("Get specific product by id")
        .WithTags("Products")
        .Produces<Product>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .WithDocumentName(DocumentName);

        app.MapPost("/api/products", ([FromBody] Product product, [FromServices] ProductService service) =>
        {
            service.Create(product);
            return Results.Created($"/api/products/{product.Id}", product);
        })
        .WithSummary("Create a new product")
        .WithTags("Products")
        .Produces<Product>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithDocumentName(DocumentName);

        app.MapPut("/api/products/{id:int}", (int id, [FromBody] Product product, [FromServices] ProductService service) =>
        {
            service.Update(id, product);
            return Results.NoContent();
        })
        .WithSummary("Update a product")
        .WithTags("Products")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithDocumentName(DocumentName);

        app.MapDelete("/api/products/{id:int}", (int id, [FromServices] ProductService service) =>
        {
            service.DeleteById(id);
            return Results.NoContent();
        })
        .WithSummary("Delete a product")
        .WithTags("Products")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithDocumentName(DocumentName);

        return app;
    }
}
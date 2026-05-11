using Microsoft.AspNetCore.Mvc;
using Modules.ProductModule;

namespace Modules.Products;

public static class ProductModule
{
    public const string V1DocumentName = "products-v1";

    public static IServiceCollection AddProductModule(this IServiceCollection services)
    {
        services.AddSingleton<ProductService>();

        services.AddOpenApi(V1DocumentName, options =>
        {
            options.AddDocumentTransformer(new ModuleInfoDocumentTransformer(
                title: "Products API",
                version: "v1",
                description: "Products module - version 1"));

            options.AddDocumentTransformer(
                new PathPrefixDocumentInclusionTransformer("/api/products/v1"));
        });

        return services;
    }

    public static WebApplication UseProductModule(this WebApplication app)
    {
        var v1 = app.MapGroup("/api/products/v1")
            .WithTags("Products");

        v1.MapGet("/", ([FromServices] ProductService service) =>
        {
            return Results.Ok(service.GetAll());
        })
        .WithName("Products_GetAll_V1")
        .WithSummary("Get all products")
        .WithDescription("Returns all products in Products module version 1.")
        .Produces<List<Product>>(StatusCodes.Status200OK);

        v1.MapGet("/{id:int}", (int id, [FromServices] ProductService service) =>
        {
            var product = service.FindById(id);

            if (product is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Product not found",
                    Detail = $"No product found with id {id}",
                    Status = StatusCodes.Status404NotFound,
                    Type = "/problems/product-not-found"
                });
            }

            return Results.Ok(product);
        })
        .WithName("Products_GetById_V1")
        .WithSummary("Get product by id")
        .WithDescription("Returns one product by id in Products module version 1.")
        .Produces<Product>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        v1.MapPost("/", ([FromBody] Product product, [FromServices] ProductService service) =>
        {
            service.Create(product);
            return Results.Created($"/api/products/v1/{product.Id}", product);
        })
        .WithName("Products_Create_V1")
        .WithSummary("Create product")
        .WithDescription("Creates a product in Products module version 1.")
        .Produces<Product>(StatusCodes.Status201Created);

        return app;
    }
}
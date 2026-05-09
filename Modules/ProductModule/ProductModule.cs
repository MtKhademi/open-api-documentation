using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Modules.ProductModule;

public static class ProductModule
{

    public static IServiceCollection AddProductModule(this IServiceCollection services)
    {
        services.AddScoped<ProductService>();

        return services;
    }

    public static WebApplication UseProductModule(this WebApplication app)
    {

        app.MapGet("/api/products", ([FromServices] ProductService service) => service.GetAll())
            .WithSummary("Get all products")
            .WithDescription("with this api ypu can get all productas")
            .WithName("GetAllProducts")
            .WithTags("Products")
            .Produces<List<Product>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesValidationProblem(StatusCodes.Status204NoContent);

        app.MapGet("/api/products/{id:int}", (int id, [FromServices] ProductService service) =>
        {
            if (id <= 0)
            {
                return Results.Problem(
                    title: "Invalid id",
                    detail: "Product id must be greater than zero.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var entity = service.FindById(id);
            if (entity == null)
            {
                return Results.Problem(
                    title: "Product not found",
                    detail: $"No product found with id {id}.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(entity);
        })
            .WithSummary("Get specific product by id")
            .WithName("GetProductById")
            .WithTags("Products")
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<List<Product>>(StatusCodes.Status200OK);


        app.MapGet("/api/products2/{id:int}",
            Results<BadRequest, NotFound, Ok<Product>> (int id, [FromServices] ProductService service) =>
            {
                if (id <= 0)
                {
                    return TypedResults.BadRequest();
                }

                var entity = service.FindById(id);
                if (entity == null)
                {
                    return TypedResults.NotFound();
                }

                return TypedResults.Ok(entity);
            })
            .WithName("GetProductById2")
            .WithTags("Products")
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<List<Product>>(StatusCodes.Status200OK);

        // app.MapGet("/api/products2/{id:int}", (int id, [FromServices] ProductService service) =>
        // {
        //     if (id <= 0)
        //     {
        //         // return TypedResults.Problem(
        //         //     title: "Invalid id",
        //         //     detail: "Product id must be greater than zero.",
        //         //     statusCode: StatusCodes.Status400BadRequest);
        //         return TypedResults.BadRequest();
        //     }

        //     var entity = service.FindById(id);
        //     if (entity == null)
        //     {
        //         // return Results.Problem(
        //         //     title: "Product not found",
        //         //     detail: $"No product found with id {id}.",
        //         //     statusCode: StatusCodes.Status404NotFound);
        //         return TypedResults.NotFound();
        //     }

        //     return TypedResults.Ok(entity);
        // })
        // // .WithSummary("Get specific product by id")
        // .WithName("GetProductById")
        // .WithTags("Products");
        // .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        // .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        // .Produces<List<Product>>(StatusCodes.Status200OK);



        // app.MapDelete("/api/products/{id:int}", (int id, [FromServices] ProductService service) =>
        // {
        //     service.DeleteById(id);
        // }).WithSummary("delete a product")
        // .WithDescription("If exist delete and can't undo")
        // .WithTags("Products");

        // app.MapDelete("/api/products/{id:int}", (int id, [FromServices] ProductService service) =>
        // {
        //     service.DeleteById(id);
        //     return Results.NoContent(); // or Results.Ok()
        // })
        // .WithSummary("delete a product")
        // .WithDescription("If exist delete and can't undo")
        // .WithTags("Products");

        // app.MapPost("/api/products", ([FromBody] Product pro, [FromServices] ProductService service) =>
        // {
        //     service.Create(pro);
        // }).WithSummary("create a new product")
        // .WithName("CreateAProduct")
        // .WithTags("Products");

        // app.MapPut("/api/products/{id:int}", (int id, [FromBody] Product pro, [FromServices] ProductService service) =>
        // {
        //     service.Update(id, pro);
        // }).WithSummary("Update a product")
        // .WithTags("Products");

        return app;

    }

}

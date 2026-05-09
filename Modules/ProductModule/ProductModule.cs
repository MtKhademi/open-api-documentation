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

        app.MapGet("/api/products", ([FromServices] ProductService service) =>
        {
            return Results.Ok(service.GetAll());
        })
        .WithSummary("Get all products")
        .WithDescription("with this api ypu can get all productas")
        .WithName("GetAllProducts")
        .WithTags("Products")
        .Produces<List<Product>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
        // .Produces<ApiResult>(StatusCodes.Status500InternalServerError);


        app.MapGet("/api/products/{id:int}", (int id, [FromServices] ProductService service) =>
        {

            if (id <= 0)
            {
                var validationProblem = new HttpValidationProblemDetails();
                validationProblem.Errors.Add(nameof(id), [
                    $"{nameof(id)} can not be zero.",
                    $"{nameof(id)} can be negative."
                ]);

                return Results.BadRequest(validationProblem);
            }


            var entity = service.FindById(id);
            if (entity is null)
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not found this product",
                    Detail = $"Not found such as product with id : {id}",
                    Type = "/api/product/id/not-found"
                };

                return Results.NotFound(problemDetails);
            }
            return Results.Ok(entity);

        }).WithSummary("Get specific product by id")
        .WithName("GetProductById")
        .WithTags("Products")
        .Produces<Product>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest);


        app.MapDelete("/api/products/{id:int}", (int id, [FromServices] ProductService service) =>
        {
            service.DeleteById(id);
        }).WithSummary("delete a product")
        .WithDescription("If exist delete and can't undo")
        .WithTags("Products");

        app.MapPost("/api/products", ([FromBody] Product pro, [FromServices] ProductService service) =>
        {
            service.Create(pro);
        }).WithSummary("create a new product")
        .WithName("CreateAProduct")
        .WithTags("Products");

        app.MapPut("/api/products/{id:int}", (int id, [FromBody] Product pro, [FromServices] ProductService service) =>
        {
            service.Update(id, pro);
        }).WithSummary("Update a product")
        .WithTags("Products");

        return app;

    }

}


internal class ApiResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}
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
        .WithTags("Products");

        app.MapGet("/api/products/{id:int}", (int id, [FromServices] ProductService service) =>
        {
            service.FindById(id);
        }).WithSummary("Get specific product by id")
        .WithName("GetProductById")
        .WithTags("Products");


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

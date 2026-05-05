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

        app.MapGet("/api/products", (
            [FromServices] ProductService service
        ) => service.GetAll());

        return app;

    }

}
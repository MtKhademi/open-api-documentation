using Modules.IAM;
using Modules.ProductModule;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProductModule();
builder.Services.AddIAMModule();

var app = builder.Build();

app.UseProductModule();
app.UseIAMModule();

// Expose all named OpenAPI documents from one place
app.MapOpenApi("/openapi/{documentName}.json");

// Expose one Scalar UI from one place
app.MapScalarApiReference("/docs", options =>
{
    options.Title = "Modular API Docs";
    options.Layout = ScalarLayout.Classic;

    options.AddDocument("products-v1", "Products API v1",
        routePattern: "/openapi/products-v1.json");

    options.AddDocument("iam-v1", "IAM API v1",
        routePattern: "/openapi/iam-v1.json");

    options.AddDocument("iam-v2", "IAM API v2",
        routePattern: "/openapi/iam-v2.json");
});

app.Run();
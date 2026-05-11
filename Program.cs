using Modules.IAM;
using Modules.Products;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddIAMModule();
builder.Services.AddProductModule();

var app = builder.Build();

app.MapOpenApi("/openapi/{documentName}.json");

app.MapScalarApiReference("/docs", options =>
{
    options.Title = "Modular Monolith API Docs";

    options.AddDocument(IAMModule.V1DocumentName, "IAM API v1", "/openapi/iam-v1.json");
    options.AddDocument(IAMModule.V2DocumentName, "IAM API v2", "/openapi/iam-v2.json");
    options.AddDocument(ProductModule.V1DocumentName, "Products API v1", "/openapi/products-v1.json");
});

app.UseIAMModule();
app.UseProductModule();

app.Run();
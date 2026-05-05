using Modules.ProductModule;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);


// add openapi service
builder.Services.AddOpenApi();

builder.Services.AddProductModule();

var app = builder.Build();

// use open api
app.MapOpenApi();
app.MapScalarApiReference();

app.UseProductModule();

app.Run();


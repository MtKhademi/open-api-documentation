using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

namespace Modules.IAM;

public static class IAMModule
{
    public const string V1DocumentName = "iam-v1";
    public const string V2DocumentName = "iam-v2";

    public static IServiceCollection AddIAMModule(this IServiceCollection services)
    {
        services.AddOpenApi(V1DocumentName, options =>
        {
            options.AddDocumentTransformer(new ModuleInfoDocumentTransformer(
                title: "IAM API",
                version: "v1",
                description: "Identity and Access Management module - version 1"));

            options.AddDocumentTransformer(new DocumentInclusionTransformer(V1DocumentName));
        });

        services.AddOpenApi(V2DocumentName, options =>
        {
            options.AddDocumentTransformer(new ModuleInfoDocumentTransformer(
                title: "IAM API",
                version: "v2",
                description: "Identity and Access Management module - version 2"));

            options.AddDocumentTransformer(new DocumentInclusionTransformer(V2DocumentName));
        });

        services.AddOpenApi(options =>
        {
            options.AddOperationTransformer(async (operation, context, cancellationToken) =>
            {
                // Generate schema for error responses
                var errorSchema = await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), null, cancellationToken);
                context.Document?.AddComponent("Error", errorSchema);

                operation.Responses ??= new OpenApiResponses();
                // Add a "4XX" response to the operation with the newly created schema
                operation.Responses["4XX"] = new OpenApiResponse
                {
                    Description = "Bad Request",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/problem+json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchemaReference("Error", context.Document)
                        }
                    }
                };
            });
        });

        return services;
    }

    public static WebApplication UseIAMModule(this WebApplication app)
    {
        var iamGroup = app.MapGroup("/api/iam").WithTags("IAM");

        iamGroup.MapPost("/v1/login", () =>
        {
            return Results.Ok(new { Message = "IAM v1 login" });
        })
        .WithSummary("Login in system")
        .WithDescription("Returns IAM login result for version 1.")
        .WithTags("IAM")
        .WithName("LoginV1")
        .WithDocumentName(V1DocumentName);

        iamGroup.MapPost("/v2/login", () =>
        {
            return Results.Ok(new { Message = "IAM v2 login" });
        })
        .WithSummary("Login in system")
        .WithDescription("Returns IAM login result for version 2.")
        .WithTags("IAM")
        .WithName("LoginV2")
        .WithDocumentName(V2DocumentName);

        iamGroup.MapPost("/v1/register", () =>
        {
            return Results.Ok(new { Message = "IAM v1 register" });
        })
        .WithSummary("Register in system")
        .WithDescription("Registers a user for version 1.")
        .WithTags("IAM")
        .WithName("RegisterV1")
        .WithDocumentName(V1DocumentName);

        iamGroup.MapPost("/v2/register", () =>
        {
            return Results.Ok(new { Message = "IAM v2 register" });
        })
        .WithSummary("Register in system")
        .WithDescription("Registers a user for version 2.")
        .WithTags("IAM")
        .WithName("RegisterV2")
        .WithDocumentName(V2DocumentName);

        return app;
    }
}
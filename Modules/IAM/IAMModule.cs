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

            options.AddDocumentTransformer(
                new PathPrefixDocumentInclusionTransformer("/api/iam/v1"));
        });

        services.AddOpenApi(V2DocumentName, options =>
        {
            options.AddDocumentTransformer(new ModuleInfoDocumentTransformer(
                title: "IAM API",
                version: "v2",
                description: "Identity and Access Management module - version 2"));

            options.AddDocumentTransformer(
                new PathPrefixDocumentInclusionTransformer("/api/iam/v2"));
        });

        return services;
    }

    public static WebApplication UseIAMModule(this WebApplication app)
    {
        var v1 = app.MapGroup("/api/iam/v1")
            .WithTags("IAM");

        v1.MapPost("/login", () =>
        {
            return Results.Ok(new
            {
                Message = "IAM v1 login successful"
            });
        })
        .WithName("IAM_Login_V1")
        .WithSummary("Login in IAM v1")
        .WithDescription("Authenticates a user in IAM module version 1.")
        .Produces(StatusCodes.Status200OK);

        v1.MapPost("/register", () =>
        {
            return Results.Ok(new
            {
                Message = "IAM v1 register successful"
            });
        })
        .WithName("IAM_Register_V1")
        .WithSummary("Register in IAM v1")
        .WithDescription("Registers a user in IAM module version 1.")
        .Produces(StatusCodes.Status200OK);

        var v2 = app.MapGroup("/api/iam/v2")
            .WithTags("IAM");

        v2.MapPost("/login", () =>
        {
            return Results.Ok(new
            {
                Message = "IAM v2 login successful",
                TokenType = "Bearer"
            });
        })
        .WithName("IAM_Login_V2")
        .WithSummary("Login in IAM v2")
        .WithDescription("Authenticates a user in IAM module version 2.")
        .Produces(StatusCodes.Status200OK);

        v2.MapPost("/register", () =>
        {
            return Results.Ok(new
            {
                Message = "IAM v2 register successful",
                RequiresEmailConfirmation = true
            });
        })
        .WithName("IAM_Register_V2")
        .WithSummary("Register in IAM v2")
        .WithDescription("Registers a user in IAM module version 2.")
        .Produces(StatusCodes.Status200OK);

        return app;
    }
}
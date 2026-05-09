public static class LoginEndPoints
{
    internal static RouteGroupBuilder AddLoginEndpoints(this RouteGroupBuilder iamGroup)
    {
        iamGroup.MapPost("/v1/login", (ILogger logger) =>
        {
            logger.LogInformation("call login v1");
        })
        .WithName("LoginV1")
        .WithSummary("login in system")
        .WithDescription("Returns IAM users for version 1.")
        .WithDocumentName("iam-v1");

        iamGroup.MapPost("/v2/login", (ILogger logger) =>
        {
            logger.LogInformation("call login v2");
        })
        .WithName("LoginV2")
        .WithSummary("login in system")
        .WithDescription("Returns IAM users for version 1.")
        .WithDocumentName("iam-v2");

        return iamGroup;
    }
}
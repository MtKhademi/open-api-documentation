internal static class RegisterEndPoints
{
    internal static RouteGroupBuilder AddRegisterEndPoints(this RouteGroupBuilder group)
    {
        group.MapPost("/v1/register", (ILogger logger) =>
        {
            logger.LogWarning("call register endpoints");
        });
        return group;
    }
}
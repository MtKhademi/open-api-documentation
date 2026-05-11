using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

public sealed class ModuleInfoDocumentTransformer(
    string title,
    string version,
    string description) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = title,
            Version = version,
            Description = description
        };

        return Task.CompletedTask;
    }
}
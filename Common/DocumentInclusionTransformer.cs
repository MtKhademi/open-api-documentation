using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

internal sealed class DocumentInclusionTransformer(string documentName)
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var pathsToRemove = new List<string>();

        foreach (var path in document.Paths)
        {
            var keepAnyOperation = false;

            foreach (var operation in path.Value.Operations)
            {
                var endpointDescription = context.DescriptionGroups
                    .SelectMany(group => group.Items)
                    .FirstOrDefault(item =>
                        string.Equals(item.RelativePath, path.Key.TrimStart('/'), StringComparison.OrdinalIgnoreCase));

                var metadata = endpointDescription?.ActionDescriptor?.EndpointMetadata
                    ?.OfType<OpenApiDocumentNameMetadata>()
                    .FirstOrDefault();

                if (metadata?.DocumentName == documentName)
                {
                    keepAnyOperation = true;
                    break;
                }
            }

            if (!keepAnyOperation)
            {
                pathsToRemove.Add(path.Key);
            }
        }

        foreach (var path in pathsToRemove)
        {
            document.Paths.Remove(path);
        }

        return Task.CompletedTask;
    }
}
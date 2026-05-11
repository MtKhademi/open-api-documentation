using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

internal sealed class PathPrefixDocumentInclusionTransformer(params string[] allowedPrefixes)
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        // 1) Keep only matching paths
        var pathsToRemove = document.Paths
            .Where(path => !allowedPrefixes.Any(prefix =>
                path.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .Select(path => path.Key)
            .ToList();

        foreach (var path in pathsToRemove)
        {
            document.Paths.Remove(path);
        }

        // 2) Keep only tags used by remaining operations
        var usedTagNames = document.Paths
            .SelectMany(path => path.Value.Operations.Values)
            .SelectMany(operation => operation.Tags?.ToList() ?? [])
            .Select(tag => tag.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // if (document.Tags is not null)
        // {
        //     document.Tags = document.Tags.ToList()
        //         .Where(tag => usedTagNames.Contains(tag.Name))
        //         .ToList();
        // }

        return Task.CompletedTask;
    }
}
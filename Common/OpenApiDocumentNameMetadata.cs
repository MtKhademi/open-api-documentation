public sealed record OpenApiDocumentNameMetadata(string DocumentName);


public static class OpenApiDocumentExtensions
{
    public static TBuilder WithDocumentName<TBuilder>(this TBuilder builder, string documentName)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.WithMetadata(new OpenApiDocumentNameMetadata(documentName));
        return builder;
    }
}
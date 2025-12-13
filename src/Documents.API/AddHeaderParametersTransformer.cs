using Asp.Versioning;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Documents.API;

/// <inheritdoc />
public class AddHeaderParametersTransformer : IOpenApiOperationTransformer
{
    /// <inheritdoc />
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        string version = context.Document?.Info?.Version;
        if (version is not null)
        {
            operation.Parameters ??= [];
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "api-version",
                In = ParameterLocation.Header,
                Required = true,
                Schema = new OpenApiSchema { Type = JsonSchemaType.String, Default = version }
            });
        }

        return Task.CompletedTask;
    }
}
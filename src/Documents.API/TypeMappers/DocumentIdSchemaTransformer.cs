using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using NJsonSchema;

namespace Documents.API.TypeMappers;


///<inheritdoc/>
public class DocumentIdSchemaTransformer : IOpenApiSchemaTransformer
{
    ///<inheritdoc/>
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        schema.Type = JsonSchemaType.String;
        schema.Format = JsonFormatStrings.Guid;
        return Task.CompletedTask;
    }
}
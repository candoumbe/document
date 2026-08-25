using System.Numerics;
using Candoumbe.Types.Numerics;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using NJsonSchema;

namespace Documents.API.TypeMappers;

/// <summary>
/// Transforms the OpenAPI schema for <see cref="Number{TNumber}"/>s.
/// </summary>
/// <typeparam name="TNumber">The current open api type</typeparam>
/// <typeparam name="TValue">The underlying numeric type</typeparam>
public class NumberTypeSchemaTransformer<TNumber, TValue> : IOpenApiSchemaTransformer
    where TNumber : Number<TValue>, IMinMaxValue<TNumber>
    where TValue : IComparable<TValue>, IMinMaxValue<TValue>
{
    ///<inheritdoc/>
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if(context.JsonTypeInfo.Type != typeof(TNumber))
        {
            return Task.CompletedTask;
        }

        switch (typeof(TValue))
        {
            case var type when type == typeof(long):
                schema.Type = JsonSchemaType.Number;
                schema.Minimum = TNumber.MinValue.Value.ToString();
                schema.Maximum = TNumber.MaxValue.Value.ToString();
                break;
            case var type when type == typeof(int):
                schema.Type = JsonSchemaType.Integer;
                schema.Minimum = TNumber.MinValue.Value.ToString();
                schema.Maximum = TNumber.MaxValue.Value.ToString();
                break;
            default:
                schema.Type = JsonSchemaType.Number;
                schema.Format = JsonFormatStrings.Integer;
                break;
        }

        return Task.CompletedTask;
    }
}

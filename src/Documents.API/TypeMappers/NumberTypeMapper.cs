using System.Numerics;
using Candoumbe.Types.Numerics;
using NJsonSchema;
using NJsonSchema.Generation.TypeMappers;

namespace Documents.API.TypeMappers;

/// <summary>
/// Mappers for <see cref="Number{TNumber}"/>s.
/// </summary>
/// <typeparam name="TValue">Type of the underlying number</typeparam>
/// <typeparam name="TNumber"></typeparam>
public class NumberTypeMapper<TNumber, TValue> : ITypeMapper
    where TNumber : Number<TValue>, IMinMaxValue<TNumber>
    where TValue : IComparable<TValue>, IMinMaxValue<TValue>
{
    /// <inheritdoc />
    void ITypeMapper.GenerateSchema(JsonSchema schema, TypeMapperContext context)
    {
        switch (typeof(TValue))
        {
            case var type when type == typeof(long):
                (schema.Type, schema.Format, schema.Minimum, schema.Maximum) = (JsonObjectType.Number, JsonFormatStrings.Long, Convert.ToDecimal(TNumber.MinValue.Value), Convert.ToDecimal(TNumber.MaxValue.Value));
                break;
            case var type when type == typeof(int) || type == typeof(short):
                (schema.Type, schema.Format, schema.Minimum, schema.Maximum) = (JsonObjectType.Integer, JsonFormatStrings.Integer, Convert.ToDecimal(TNumber.MinValue.Value), Convert.ToDecimal(TNumber.MaxValue.Value));
                break;
            default:
                (schema.Type, schema.Format) = (JsonObjectType.Number, JsonFormatStrings.Integer);
                break;
        }
    }

    /// <inheritdoc />
    Type ITypeMapper.MappedType => typeof(TNumber);

    /// <inheritdoc />
    bool ITypeMapper.UseReference => false;
}
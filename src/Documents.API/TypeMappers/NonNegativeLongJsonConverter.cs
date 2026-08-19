using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Candoumbe.Types.Numerics;

namespace Documents.API.TypeMappers;

/// <summary>
/// Serializes <see cref="NonNegativeLong"/> as a plain JSON number instead of its default object shape.
/// </summary>
public sealed class NonNegativeLongJsonConverter : JsonConverter<NonNegativeLong>
{
    ///<inheritdoc />
    public override NonNegativeLong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => NonNegativeLong.From(reader.GetInt64());

    ///<inheritdoc />
    public override void Write(Utf8JsonWriter writer, NonNegativeLong value, JsonSerializerOptions options)
        => writer.WriteNumberValue((long)value);
}

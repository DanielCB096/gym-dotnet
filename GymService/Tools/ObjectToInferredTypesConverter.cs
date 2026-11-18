using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GymService.Tools
{
    /// <summary>
    /// Convierte valores JSON deserializados como <c>object</c> a tipos CLR primitivos
    /// (long, decimal, bool, string, List&lt;object?&gt;, Dictionary&lt;string, object?&gt;).
    ///
    /// Sin este convertidor, System.Text.Json devuelve <see cref="JsonElement"/> y las llamadas
    /// a <c>Convert.ToInt64</c>/<c>Convert.ToDecimal</c> de los módulos lanzan
    /// <see cref="InvalidCastException"/> al recibir un <c>Dictionary&lt;string, object&gt;</c>.
    /// </summary>
    public sealed class ObjectToInferredTypesConverter : JsonConverter<object>
    {
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                    return true;

                case JsonTokenType.False:
                    return false;

                case JsonTokenType.Null:
                    return null;

                case JsonTokenType.String:
                    return reader.GetString();

                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out var integer))
                        return integer;
                    if (reader.TryGetDecimal(out var decimalValue))
                        return decimalValue;
                    return reader.GetDouble();

                case JsonTokenType.StartArray:
                {
                    var list = new List<object?>();
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndArray)
                            break;
                        list.Add(Read(ref reader, typeof(object), options));
                    }
                    return list;
                }

                case JsonTokenType.StartObject:
                {
                    var dictionary = new Dictionary<string, object?>();
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndObject)
                            break;

                        var key = reader.GetString() ?? string.Empty;
                        reader.Read();
                        dictionary[key] = Read(ref reader, typeof(object), options);
                    }
                    return dictionary;
                }

                default:
                {
                    // Último recurso: conservar el JSON crudo como JsonElement clonado.
                    using var document = JsonDocument.ParseValue(ref reader);
                    return document.RootElement.Clone();
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            var runtimeType = value.GetType();
            if (runtimeType == typeof(object))
            {
                writer.WriteStartObject();
                writer.WriteEndObject();
                return;
            }

            JsonSerializer.Serialize(writer, value, runtimeType, options);
        }
    }
}

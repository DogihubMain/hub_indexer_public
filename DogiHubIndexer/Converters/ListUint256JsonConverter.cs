using System.Text.Json;
using System.Text.Json.Serialization;
using NBitcoin;

namespace DogiHubIndexer.Converters
{
    public class ListUint256JsonConverter : JsonConverter<List<uint256>>
    {
        public override List<uint256> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var list = new List<uint256>();

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Expected StartArray token");
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return list;
                }

                string stringValue = reader.GetString()!;
                var value = uint256.Parse(stringValue);
                list.Add(value);
            }

            throw new JsonException("Expected EndArray token");
        }

        public override void Write(Utf8JsonWriter writer, List<uint256> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in value)
            {
                writer.WriteStringValue(item.ToString());
            }
            writer.WriteEndArray();
        }
    }
}
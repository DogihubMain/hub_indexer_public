using NBitcoin;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DogiHubIndexer.Converters
{
    public class Uint256JsonConverter : JsonConverter<uint256>
    {
        public override uint256 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string stringValue = reader.GetString()!;
            return uint256.Parse(stringValue);
        }

        public override void Write(Utf8JsonWriter writer, uint256 value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}

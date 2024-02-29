using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZstdNet;

namespace DogiHubIndexer.Helpers
{
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull 
        };

        public static string Serialize<T>(T obj)
        {
            return JsonSerializer.Serialize(obj, Options);
        }

        public static T Deserialize<T>(string serialized)
        {
            return JsonSerializer.Deserialize<T>(serialized, Options) ?? throw new JsonException("Deserialization failed");
        }

        public static byte[] SerializeAndCompress<T>(T obj)
        {
            string json = JsonSerializer.Serialize(obj, Options);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            using (var compressor = new Compressor())
            {
                return compressor.Wrap(jsonBytes);
            }
        }

        public static T DecompressAndDeserialize<T>(byte[] compressed)
        {
            using (var decompressor = new Decompressor())
            {
                byte[] jsonBytes = decompressor.Unwrap(compressed);
                string json = Encoding.UTF8.GetString(jsonBytes);
                return JsonSerializer.Deserialize<T>(json, Options) ?? throw new JsonException("Deserialization failed");
            }
        }
    }
}

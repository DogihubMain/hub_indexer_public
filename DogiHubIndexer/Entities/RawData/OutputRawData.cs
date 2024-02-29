using DogiHubIndexer.Entities.Interfaces;
using System.Text.Json.Serialization;

namespace DogiHubIndexer.Entities.RawData
{
    public record OutputRawData : IRawDataEntity
    {
        [JsonPropertyName("i")]
        public required string InscriptionId { get; init; }
        [JsonPropertyName("a")]
        public required string? RelatedAddress { get; set; }
    }
}

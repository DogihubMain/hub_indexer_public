using DogiHubIndexer.Entities.Interfaces;
using DogiHubIndexer.Helpers;
using System.Text.Json.Serialization;

namespace DogiHubIndexer.Entities.ReadModels
{
    public record TokenReadModel : IReadModelEntity
    {
        private string _tick;

        [JsonIgnore]
        public string Tick
        {
            get => _tick;
            set => _tick = value.ToLowerInvariant();
        }
        [JsonPropertyName("m")]
        public decimal? Max { get; set; }
        [JsonPropertyName("l")]
        public decimal? Lim { get; set; }
        [JsonPropertyName("s")]
        public decimal CurrentSupply { get; set; }
        [JsonIgnore]
        public string Protocol { get; set; } = "drc-20";
        [JsonPropertyName("t")]
        public required string TransactionHash { get; set; }
        [JsonPropertyName("d")]
        public DateTimeOffset Date { get; set; }
        [JsonPropertyName("e")]
        public int? Decimal { get; set; }
        [JsonPropertyName("b")]
        public required string DeployedBy { get; set; }

        public void IncreaseCurrentSupply(decimal amount)
        {
            CurrentSupply = CurrentSupply + amount;
        }

        public static TokenReadModel? Build(string? json, string tick)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            var readModel = JsonHelper.Deserialize<TokenReadModel>(json!);

            if (readModel != null)
            {
                readModel.Tick = tick;
            }
            return readModel;
        }
    }
}

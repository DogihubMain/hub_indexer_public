using DogiHubIndexer.Entities.Interfaces;
using DogiHubIndexer.Helpers;
using System.Text.Json.Serialization;

namespace DogiHubIndexer.Entities.ReadModels
{
    public record UserBalanceTokenReadModel : IReadModelEntity
    {
        private string _tokenTick;

        [JsonIgnore]
        public string Address { get; set; }
        [JsonIgnore]
        public string TokenTick
        {
            get => _tokenTick;
            set => _tokenTick = value.ToLowerInvariant();
        }
        [JsonPropertyName("a")]
        public decimal? Available { get; set; }
        [JsonPropertyName("t")]
        public decimal? Transferable { get; set; }
        [JsonPropertyName("p")]
        public decimal? Pending { get; set; }

        [JsonIgnore]
        public decimal BalanceSum { get { return Available.GetValueOrDefault(0) + Transferable.GetValueOrDefault(0) + Pending.GetValueOrDefault(0); } }

        public void ShiftAvailableToTransferableBalance(decimal amount)
        {
            Available = Available.GetValueOrDefault(0) - amount;
            Transferable = Transferable.GetValueOrDefault(0) + amount;
        }

        public void IncreaseBalance(decimal amount)
        {
            Available = Available.GetValueOrDefault(0) + amount;
        }

        public void IncreasePendingBalance(decimal amount)
        {
            Pending = Pending.GetValueOrDefault(0) + amount;
        }

        public void IncreaseConfirmedBalance(decimal amount)
        {
            Available = Available.GetValueOrDefault(0) + amount;
            Pending = Pending.GetValueOrDefault(0) - amount;
        }

        public void DecreaseTransferableBalance(decimal amount)
        {
            Transferable = Transferable.GetValueOrDefault(0) - amount;
        }

        public static UserBalanceTokenReadModel? Build(string? json, string tick, string address)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            var readModel = JsonHelper.Deserialize<UserBalanceTokenReadModel>(json!);

            if (readModel != null)
            {
                readModel.TokenTick = tick;
                readModel.Address = address;
            }
            return readModel;
        }
    }
}

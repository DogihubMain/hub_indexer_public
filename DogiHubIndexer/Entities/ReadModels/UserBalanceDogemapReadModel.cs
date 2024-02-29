using DogiHubIndexer.Entities.Interfaces;
using DogiHubIndexer.Helpers;
using System.Text.Json.Serialization;

namespace DogiHubIndexer.Entities.ReadModels
{
    public record UserBalanceDogemapReadModel : IReadModelEntity
    {
        [JsonIgnore]
        public string Address { get; set; }

        [JsonPropertyName("i")]
        public required string InscriptionId { get; set; }

        [JsonPropertyName("p")]
        public required bool IsPending { get; set; }

        [JsonIgnore]
        public int Number { get; set; }

        public void ConfirmBalance()
        {
            IsPending = false;
        }

        public static UserBalanceDogemapReadModel? Build(string? json, int number, string address)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            var readModel = JsonHelper.Deserialize<UserBalanceDogemapReadModel>(json!);

            if (readModel != null)
            {
                readModel.Number = number;
                readModel.Address = address;
            }
            return readModel;
        }

    }
}

using DogiHubIndexer.Entities.Interfaces;
using DogiHubIndexer.Helpers;
using System.Text.Json.Serialization;

namespace DogiHubIndexer.Entities.ReadModels
{
    public record UserBalanceNftReadModel : IReadModelEntity
    {
        [JsonIgnore]
        public string Address { get; set; }

        [JsonIgnore]
        public string InscriptionId { get; set; }

        [JsonPropertyName("p")]
        public required bool IsPending { get; set; }

        public void ConfirmBalance()
        {
            IsPending = false;
        }

        public static UserBalanceNftReadModel? Build(string? json, string inscriptionId, string address)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            var readModel = JsonHelper.Deserialize<UserBalanceNftReadModel>(json!);

            if (readModel != null)
            {
                readModel.Address = address;
                readModel.InscriptionId = inscriptionId;
            }
            return readModel;
        }
    }
}

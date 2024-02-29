using DogiHubIndexer.Entities.Interfaces;
using DogiHubIndexer.Helpers;
using System.Text.Json.Serialization;

namespace DogiHubIndexer.Entities.ReadModels
{
    public record UserBalanceDnsReadModel : IReadModelEntity
    {
        private string _name;

        [JsonIgnore]
        public string Address { get; set; }

        [JsonIgnore]
        public string Name
        {
            get => _name;
            set => _name = value.ToLowerInvariant();
        }

        [JsonPropertyName("i")]
        public required string InscriptionId { get; set; }

        [JsonPropertyName("p")]
        public required bool IsPending { get; set; }

        public void ConfirmBalance()
        {
            IsPending = false;
        }

        public static UserBalanceDnsReadModel? Build(string? json, string name, string address)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            var readModel = JsonHelper.Deserialize<UserBalanceDnsReadModel>(json!);

            if (readModel != null)
            {
                readModel.Name = name;
                readModel.Address = address;
            }
            return readModel;
        }
    }
}

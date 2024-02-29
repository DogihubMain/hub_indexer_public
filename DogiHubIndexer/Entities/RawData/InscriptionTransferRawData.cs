using DogiHubIndexer.Converters;
using DogiHubIndexer.Entities.Interfaces;
using NBitcoin;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DogiHubIndexer.Entities.RawData
{
    public record InscriptionTransferRawData : IRawDataEntity
    {
        [JsonPropertyName("b")]
        public required ulong BlockNumber { get; set; }
        [JsonPropertyName("p")]
        public int TransactionIndex { get; set; }
        [JsonPropertyName("h")]
        [JsonConverter(typeof(Uint256JsonConverter))]
        public required uint256 TransactionHash { get; set; }
        [JsonPropertyName("x")]
        public required uint InputIndex { get; set; }
        [JsonIgnore]
        public InscriptionRawData? Inscription { get; set; }
        [JsonPropertyName("i")]
        public required string InscriptionId { get; init; }
        [JsonPropertyName("t")]
        public required InscriptionTransferType InscriptionTransferType { get; set; }
        [JsonPropertyName("r")]
        public required string Receiver { get; set; }
        [JsonPropertyName("s")]
        public string? Sender { get; set; } // only relevant if really a transfer happened
        [JsonIgnore]
        public DateTimeOffset? Date { get; set; }

        public static InscriptionTransferType? GetConfirmedTypeEquivalent(InscriptionTransferType inscriptionTransferType)
        {
            return inscriptionTransferType switch
            {
                InscriptionTransferType.PENDING_TRANSFER => InscriptionTransferType.CONFIRMED_TRANSFER,
                InscriptionTransferType.PENDING_NFT => InscriptionTransferType.CONFIRMED_NFT,
                InscriptionTransferType.PENDING_DNS => InscriptionTransferType.CONFIRMED_DNS,
                InscriptionTransferType.PENDING_DOGEMAP => InscriptionTransferType.CONFIRMED_DOGEMAP,
                _ => null
            };
        }
    }

    public enum InscriptionTransferType
    {
        [Description("d")]
        DEPLOY,
        [Description("m")]
        MINT,
        [Description("it")]
        INSCRIBE_TRANSFER,

        //PENDING ONES
        [Description("pt")]
        PENDING_TRANSFER,
        [Description("pd")]
        PENDING_DNS,
        [Description("pm")]
        PENDING_DOGEMAP,
        [Description("pn")]
        PENDING_NFT,

        [Description("ct")]
        CONFIRMED_TRANSFER,
        [Description("cd")]
        CONFIRMED_DNS,
        [Description("cm")]
        CONFIRMED_DOGEMAP,
        [Description("cn")]
        CONFIRMED_NFT,
    }
}

using DogiHubIndexer.Converters;
using DogiHubIndexer.Entities.Interfaces;
using DogiHubIndexer.Helpers;
using NBitcoin;
using System.Text.Json.Serialization;

namespace DogiHubIndexer.Entities.RawData
{
    public class InscriptionRawData : IRawDataEntity
    {
        [JsonIgnore]
        private bool IsToken { get { return TokenContent != null; } }
        [JsonIgnore]
        private bool IsDogemap { get { return DogemapContent != null; } }
        [JsonIgnore]
        private bool IsDns { get { return DnsContent != null; } }
        [JsonIgnore]
        private bool IsNft { get { return NftContent != null; } }

        [JsonIgnore]
        public InscriptionId Id { get; set; }
        [JsonPropertyName("t")]
        public TokenInscriptionContentRawData? TokenContent { get; init; }
        [JsonPropertyName("m")]
        public DogemapInscriptionContentRawData? DogemapContent { get; init; }
        [JsonPropertyName("d")]
        public DnsInscriptionContentRawData? DnsContent { get; init; }
        [JsonPropertyName("n")]
        public NftInscriptionContentRawData? NftContent { get; init; }
        [JsonIgnore]
        public uint256 GenesisTxId { get; set; }
        [JsonPropertyName("c")]
        public required string ContentType { get; init; }
        [JsonPropertyName("s")]
        public required DateTimeOffset Timestamp { get; init; }

        [JsonIgnore()]
        public InscriptionTypeEnum Type
        {
            get
            {
                if (IsToken) return InscriptionTypeEnum.Token;
                if (IsDogemap) return InscriptionTypeEnum.Dogemap;
                if (IsDns) return InscriptionTypeEnum.Dns;
                if (IsNft) return InscriptionTypeEnum.Nft;
                throw new ArgumentException("Invalid inscription type", nameof(InscriptionTypeEnum));
            }
        }

        [JsonIgnore]
        public string Name
        {
            get
            {
                if (IsToken) return TokenContent!.tick;
                if (IsDogemap) return DogemapContent!.Name;
                if (IsDns) return DnsContent!.name;
                if (IsNft) return Id.ToString();
                throw new ArgumentException("Invalid name", nameof(InscriptionTypeEnum));
            }
        }

        public static InscriptionRawData? Build(string? json, string inscriptionId)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            var readModel = JsonHelper.Deserialize<InscriptionRawData>(json!);

            if (readModel != null)
            {
                var inscriptionIdParsed = InscriptionId.Parse(inscriptionId);
                readModel.Id = inscriptionIdParsed;
                readModel.GenesisTxId = inscriptionIdParsed.Txid;
            }
            return readModel;
        }
    }

    public enum InscriptionTypeEnum
    {
        Token,
        Dogemap,
        Dns,
        Nft
    }

    public class TokenInscriptionContentRawData
    {
        private string _tick;

        [JsonIgnore]
        public string p { get; set; }
        public required string op { get; set; }

        public required string tick
        {
            get => _tick;
            set => _tick = value.ToLowerInvariant();
        }
        public string? amt { get; set; }
        public string? max { get; set; }
        public string? lim { get; set; }
        public string? dec { get; set; }
    }

    public class DogemapInscriptionContentRawData
    {
        [JsonPropertyName("n")]
        public required string Name { get; set; }
        [JsonIgnore]
        public int Number { get { return int.Parse(Name.Split('.')[0]); } }
    }

    public class DnsInscriptionContentRawData
    {
        public required string p { get; set; }
        public required string op { get; set; }
        public required string name { get; set; }
    }

    public class NftInscriptionContentRawData
    {
        [JsonPropertyName("ids")]
        [JsonConverter(typeof(ListUint256JsonConverter))]
        public required List<uint256> TxIds { get; set; }
        //allow us if all transactions ids has already been get
        [JsonPropertyName("c")]
        public required bool IsComplete { get; set; } = false;
    }
}

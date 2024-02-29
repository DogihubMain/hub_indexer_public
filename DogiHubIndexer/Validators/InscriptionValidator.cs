using DogiHubIndexer.Entities;
using DogiHubIndexer.Entities.RawData;
using NBitcoin;
using System.Globalization;
using System.Text.Json;

namespace DogiHubIndexer.Validators
{
    public class InscriptionValidator
    {
        private const string DRC_20_SYMBOLS = "drc-20";
        private const string DNS_SYMBOL = "dns";
        private const string OpDeploy = "deploy";
        private const string OpMint = "mint";
        public const string OpTransfer = "transfer";
        private const string OpReg = "reg";
        private const string DogemapExtension = ".dogemap";
        private const int DogemapNumberMax = 5000000;
        private static readonly string[] DnsExtensionsAllowed = new string[] { "x", "doge", "hub", "oifi" };

        public bool TryGetTokenInscriptionContent(
            DateTimeOffset blockTime,
            JsonDocument jsonDocument,
            uint256 genesisTxId,
            InscriptionId inscriptionId,
            string mimeType,
            out InscriptionRawData? inscriptionRawData)
        {
            inscriptionRawData = null;

            var root = jsonDocument.RootElement;
            var p = GetNullableJsonField(root, "p");
            var op = GetNullableJsonField(root, "op");
            var tick = GetNullableJsonField(root, "tick");
            var amt = GetNullableJsonField(root, "amt");
            var lim = GetNullableJsonField(root, "lim");
            var max = GetNullableJsonField(root, "max");

            if (p == null || !p.Equals(DRC_20_SYMBOLS, StringComparison.Ordinal))
                return false;

            if (!new[] { OpDeploy, OpMint, OpTransfer }.Contains(op))
                return false;

            if (string.IsNullOrEmpty(tick))
                return false;

            //only tick with a lenght of 4 or specificaly 𝕏 are allowed 
            if (tick.Length != 4 && tick != "𝕏")
                return false;

            var isOpValid = false;

            //manual deserialization for performances
            var inscriptionContent = new TokenInscriptionContentRawData
            {
                p = p,
                op = op!,
                tick = tick,
                amt = amt,
                lim = lim,
                max = max
            };

            if (op == OpDeploy)
            {
                isOpValid = IsDeployValid(inscriptionContent);
            }

            if (op == OpMint)
            {
                isOpValid = IsMintValid(inscriptionContent);
            }

            if (op == OpTransfer)
            {
                isOpValid = IsTransferValid(inscriptionContent);
            }

            if (isOpValid)
            {
                inscriptionRawData = new InscriptionRawData()
                {
                    TokenContent = inscriptionContent,
                    GenesisTxId = genesisTxId,
                    Id = inscriptionId,
                    ContentType = mimeType,
                    Timestamp = blockTime
                };
            }

            return isOpValid;
        }

        public bool TryGetDnsInscriptionContent(
            DateTimeOffset blockTime,
            JsonDocument jsonDocument,
            uint256 genesisTxId,
            InscriptionId inscriptionId,
            string fullMimeType,
            out InscriptionRawData? inscriptionRawData)
        {
            inscriptionRawData = null;

            var root = jsonDocument.RootElement;
            var p = GetNullableJsonField(root, "p");
            var op = GetNullableJsonField(root, "op");
            var name = GetNullableJsonField(root, "name");

            if (p == null || !p.Equals(DNS_SYMBOL, StringComparison.Ordinal))
                return false;

            if (op == null || !new[] { OpReg }.Contains(op))
                return false;

            if (name == null) return false;

            var inscriptionContent = new DnsInscriptionContentRawData()
            {
                p = p,
                name = name,
                op = op
            };

            var isOpValid = IsRegValid(inscriptionContent);

            if (isOpValid)
            {
                inscriptionRawData = new InscriptionRawData()
                {
                    DnsContent = inscriptionContent,
                    GenesisTxId = genesisTxId,
                    Id = inscriptionId,
                    ContentType = fullMimeType,
                    Timestamp = blockTime
                };
            }
            return isOpValid;
        }

        public bool TryGetNftInscriptionContent(
            ulong blockNumber,
            DateTimeOffset blockTime,
            uint256 genesisTxId,
            InscriptionId inscriptionId,
            string contentType,
            List<uint256> transactionIds,
            bool isComplete,
            out InscriptionRawData? inscriptionRawData)
        {
            //no control because we are storing all mimetypes
            inscriptionRawData = new InscriptionRawData()
            {
                GenesisTxId = genesisTxId,
                Id = inscriptionId,
                ContentType = contentType,
                NftContent = new NftInscriptionContentRawData()
                {
                    IsComplete = isComplete,
                    TxIds = transactionIds
                },
                Timestamp = blockTime
            };

            return true;
        }

        public bool TryGetDogemapInscriptionContent(
            ulong blockNumber,
            DateTimeOffset blockTime,
            string content,
            uint256 genesisTxId,
            InscriptionId inscriptionId,
            string mimeType,
            out InscriptionRawData? inscriptionRawData)
        {
            inscriptionRawData = null;
            if (!content.ToLower().Contains(DogemapExtension)) return false;
            if (!int.TryParse(content.Split(".")[0], out int dogemapNumber)) return false;
            if (dogemapNumber > DogemapNumberMax) return false;

            inscriptionRawData = new InscriptionRawData()
            {
                DogemapContent = new DogemapInscriptionContentRawData() { Name = content },
                GenesisTxId = genesisTxId,
                Id = inscriptionId,
                ContentType = mimeType,
                Timestamp = blockTime
            };
            return true;
        }

        private static bool IsAPositiveDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var styles = NumberStyles.AllowLeadingWhite
                         | NumberStyles.AllowTrailingWhite
                         | NumberStyles.AllowDecimalPoint;

            return decimal.TryParse(value, styles, CultureInfo.InvariantCulture, out decimal parsedValue) && parsedValue > 0;
        }

        #region Deploy

        private static bool IsDeployValid(TokenInscriptionContentRawData inscriptionContent)
        {
            return IsDeployMaxValid(inscriptionContent) && IsDeployLimValid(inscriptionContent);
        }

        private static bool IsDeployMaxValid(TokenInscriptionContentRawData inscriptionContent)
        {
            if (inscriptionContent.max?.Length > 20)
            {
                return false;
            }

            return IsAPositiveDecimal(inscriptionContent.max);
        }

        private static bool IsDeployLimValid(TokenInscriptionContentRawData inscriptionContent)
        {
            return IsAPositiveDecimal(inscriptionContent.lim);
        }

        #endregion

        #region Mint

        private static bool IsMintValid(TokenInscriptionContentRawData inscriptionContent)
        {
            return IsMintAmtValid(inscriptionContent);
        }

        private static bool IsMintAmtValid(TokenInscriptionContentRawData inscriptionContent)
        {
            return IsAPositiveDecimal(inscriptionContent.amt);
        }

        #endregion

        #region Transfer

        private static bool IsTransferValid(TokenInscriptionContentRawData inscriptionContent)
        {
            return IsTransferAmtValid(inscriptionContent);
        }

        private static bool IsTransferAmtValid(TokenInscriptionContentRawData inscriptionContent)
        {
            return IsAPositiveDecimal(inscriptionContent.amt);
        }


        #endregion

        #region Reg
        private static bool IsRegValid(DnsInscriptionContentRawData inscriptionContent)
        {
            return IsDnsNameValid(inscriptionContent);
        }

        private static bool IsDnsNameValid(DnsInscriptionContentRawData inscriptionContent)
        {
            var splittedName = inscriptionContent.name.Split(".");

            if (splittedName.Length != 2) return false;
            var extension = splittedName[1];

            if (string.IsNullOrWhiteSpace(extension)) return false;

            if (!DnsExtensionsAllowed.Contains(extension)) return false;

            return true;
        }

        #endregion

        private static string? GetNullableJsonField(JsonElement root, string propertyName)
        {
            string? fieldValue = null;
            if (root.TryGetProperty(propertyName, out JsonElement jsonElement))
            {
                fieldValue = jsonElement.GetString();
            }

            return fieldValue;
        }
    }
}

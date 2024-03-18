using DogiHubIndexer.Entities;
using DogiHubIndexer.Entities.RawData;
using DogiHubIndexer.Helpers;
using DogiHubIndexer.Services.Interfaces;
using DogiHubIndexer.Validators;
using NBitcoin;
using System.Globalization;
using System.Text.Json;

namespace DogiHubIndexer.Services
{
    public class InscriptionService : IInscriptionService
    {
        private readonly InscriptionValidator _inscriptionValidator;
        private readonly Options _options;

        public InscriptionService(InscriptionValidator inscriptionValidator, Options options)
        {
            _inscriptionValidator = inscriptionValidator;
            _options = options;
        }

        private const string OrdIndicator = "6f7264";
        //private readonly string[] AllowedNftMimeTypes = new string[] { "image/webp", "image/png", "image/jpeg" };
        private readonly string[] AllowedTokenMimeTypes = new string[] { "text/plain", "application/json" };
        private readonly string[] AllowedDogemapMimeTypes = new string[] { "text/plain", "application/json" };
        private readonly string[] AllowedDnsMimeTypes = new string[] { "text/plain", "application/json" };

        public InscriptionRawData? ExtractInscriptionFromScriptSig(
            ulong blockNumber,
            Block block,
            Transaction tx,
            TxIn txIn,
            uint256 genesisTxId,
            InscriptionId inscriptionId)
        {
            var scriptSigString = txIn.ScriptSig.ToString();
            if (string.IsNullOrWhiteSpace(scriptSigString)) return null;

            var scriptArray = scriptSigString.Split(" ");

            if (scriptArray[0] != OrdIndicator) return null;

            var contentType = HexConverter.HexToString(scriptArray[2]);
            var mimeTypeComponents = contentType.Split(';');
            if (mimeTypeComponents.Length == 0) return null;
            var mimeType = mimeTypeComponents[0];
            if (!MimeTypeValidator.IsMimeTypeFormatValid(mimeType)) return null;

            var content = HexConverter.HexToString(scriptArray[4]);

            InscriptionRawData? inscriptionRawData;

            if (JsonValidator.TryGetValidJson(content, out JsonDocument? jsonDocument))
            {
                //token ?
                if (CheckIfJsonDocumentIsProbablyAToken(jsonDocument!, mimeType))
                {
                    inscriptionRawData = ExtractTokenInscriptionFromScriptSig(
                        jsonDocument!,
                        contentType,
                        mimeType,
                        block.Header.BlockTime,
                        genesisTxId,
                        inscriptionId);

                    if (inscriptionRawData != null)
                    {
                        if (!IsInscriptionTypeToParse(InscriptionTypeEnum.Token))
                        {
                            return null;
                        }
                        return inscriptionRawData;
                    }
                }

                //dns ?
                if (CheckIfJsonDocumentIsProbablyADns(jsonDocument!, mimeType))
                {
                    inscriptionRawData = ExtractDnsInscriptionFromScriptSig(
                    jsonDocument!,
                    contentType,
                    mimeType,
                    block.Header.BlockTime,
                    genesisTxId,
                    inscriptionId);

                    if (inscriptionRawData != null)
                    {
                        if (!IsInscriptionTypeToParse(InscriptionTypeEnum.Dns))
                        {
                            return null;
                        }
                        return inscriptionRawData;
                    }
                }
            }

            //dogemap ?
            inscriptionRawData = ExtractDogemapInscriptionFromScriptSig(
                blockNumber,
                content,
                contentType,
                mimeType,
                block.Header.BlockTime,
                genesisTxId,
                inscriptionId);

            if (inscriptionRawData != null)
            {
                if (!IsInscriptionTypeToParse(InscriptionTypeEnum.Dogemap))
                {
                    return null;
                }
                return inscriptionRawData;
            }

            //nft ?
            inscriptionRawData = ExtractNftInscriptionFromScriptSig(
                blockNumber,
                block,
                tx,
                txIn,
                contentType,
                mimeType,
                genesisTxId,
                inscriptionId);
            if (inscriptionRawData != null)
            {
                if (!IsInscriptionTypeToParse(InscriptionTypeEnum.Nft))
                {
                    return null;
                }
                return inscriptionRawData;
            }

            return inscriptionRawData;
        }

        private bool IsInscriptionTypeToParse(InscriptionTypeEnum inscriptionType)
        {
            return _options.InscriptionTypes != null 
                && _options.InscriptionTypes.Any() 
                ? _options.InscriptionTypes.Contains(inscriptionType) 
                : true;
        }

        private bool CheckIfJsonDocumentIsProbablyAToken(JsonDocument jsonDocument, string mimeType)
        {
            if (!AllowedTokenMimeTypes.Contains(mimeType)) return false;
            if (!JsonValidator.AreFieldsStrings(jsonDocument!)) return false;
            return true;
        }

        private bool CheckIfJsonDocumentIsProbablyADns(JsonDocument jsonDocument, string mimeType)
        {
            if (!AllowedDnsMimeTypes.Contains(mimeType)) return false;
            if (!JsonValidator.AreFieldsStrings(jsonDocument!)) return false;
            return true;
        }

        private InscriptionRawData? ExtractTokenInscriptionFromScriptSig(
            JsonDocument jsonDocument,
            string contentType,
            string mimeType,
            DateTimeOffset blockTime,
            uint256 genesisTxId,
            InscriptionId inscriptionId)
        {
            //probably a type token inscription
            if (_inscriptionValidator.TryGetTokenInscriptionContent(
                blockTime,
                jsonDocument,
                genesisTxId,
                inscriptionId,
                contentType,
                out InscriptionRawData? inscriptionRawData))
            {
                return inscriptionRawData;
            }
            return null;
        }

        private InscriptionRawData? ExtractDnsInscriptionFromScriptSig(
            JsonDocument jsonDocument,
            string contentType,
            string mimeType,
            DateTimeOffset blockTime,
            uint256 genesisTxId,
            InscriptionId inscriptionId)
        {
            //probably a type dns inscription
            if (_inscriptionValidator.TryGetDnsInscriptionContent(
                blockTime,
                jsonDocument,
                genesisTxId,
                inscriptionId,
                contentType,
                out InscriptionRawData? inscriptionRawData))
            {
                return inscriptionRawData;
            }
            return null;
        }

        public InscriptionRawData? ExtractDogemapInscriptionFromScriptSig(
            ulong blockNumber,
            string content,
            string contentType,
            string mimeType,
            DateTimeOffset blockTime,
            uint256 genesisTxId,
            InscriptionId inscriptionId)
        {
            if (AllowedDogemapMimeTypes.Contains(mimeType))
            {
                //probably a type dogemap inscription
                if (_inscriptionValidator.TryGetDogemapInscriptionContent(
                    blockNumber,
                    blockTime,
                    content,
                    genesisTxId,
                    inscriptionId,
                    contentType,
                    out InscriptionRawData? inscriptionRawData))
                {
                    return inscriptionRawData;
                }
            }
            return null;
        }

        private InscriptionRawData? ExtractNftInscriptionFromScriptSig(
            ulong blockNumber,
            Block block,
            Transaction tx,
            TxIn txIn,
            string contentType,
            string mimeType,
            uint256 genesisTxId,
            InscriptionId inscriptionId)
        {
            var transactions = GetNftTransactionIdsInCurrentBlock(block, tx, txIn, out bool isComplete, isGenesis: true);
            var transactionIds = transactions.Select(x => x.GetHash()).ToList();

            //probably a type token inscription
            if (_inscriptionValidator.TryGetNftInscriptionContent(
                blockNumber,
                block.Header.BlockTime,
                genesisTxId,
                inscriptionId,
                contentType,
                transactionIds,
                isComplete,
                out InscriptionRawData? inscriptionRawData))
            {
                return inscriptionRawData;
            }
            return null;
        }

        public List<Transaction> GetNftTransactionIdsInCurrentBlock(Block block, Transaction tx, TxIn txIn, out bool isComplete, bool isGenesis = false)
        {
            isComplete = false;
            var transactions = new List<Transaction>();
            transactions.Add(tx);

            var scriptSigns = txIn.ScriptSig.ToString().Split(" ");
            var scriptSignsIndicatorsOnly = scriptSigns.Where((element, index) => isGenesis ? index % 2 != 0 : index % 2 == 0);

            var chunkNumbers = scriptSignsIndicatorsOnly
                .Select(x =>
                {
                    bool success = int.TryParse(x, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int result);
                    return success ? (int?)result : null;
                })
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();

            if (chunkNumbers.Any())
            {
                var lastSyncChunk = chunkNumbers[0];
                var increment = 1;

                if (chunkNumbers.Count > 2)
                {
                    increment = chunkNumbers[0] - chunkNumbers[1];
                    for (var i = 1; i < chunkNumbers.Count; i++)
                    {
                        if (chunkNumbers[i] == (lastSyncChunk - increment))
                        {
                            lastSyncChunk = chunkNumbers[i];
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (lastSyncChunk > 0)
                    {
                        var nextTx = FindNextRelatedTransactionInBlock(block, tx.GetHash());
                        if (nextTx != null)
                        {
                            if (nextTx.Inputs.Any())
                            {
                                var nextTxIn = nextTx.Inputs[0];
                                transactions.AddRange(GetNftTransactionIdsInCurrentBlock(block, nextTx, nextTxIn, out isComplete, isGenesis: false));
                            }
                        }
                    }
                    else
                    {
                        isComplete = true;
                    }
                }
                else
                {
                    isComplete = true;
                }
            }
            else
            {
                isComplete = true;
            }


            return transactions;
        }

        private Transaction? FindNextRelatedTransactionInBlock(Block block, uint256 transactionHash)
        {
            foreach (var transaction in block.Transactions)
            {
                if (transaction.Inputs.Any())
                {
                    var txIn = transaction.Inputs[0];
                    var previousOutputTxHash = txIn.PrevOut.Hash;
                    var previousOutputIndex = txIn.PrevOut.N;

                    if (previousOutputTxHash == transactionHash
                        && previousOutputIndex == 0)
                    {
                        return transaction;
                    }
                }
            }
            return null;
        }
    }
}

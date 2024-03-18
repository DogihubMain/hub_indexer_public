
using DogiHubIndexer.Entities.RawData;
using NBitcoin;

namespace DogiHubIndexer.Configuration
{
    public static class RedisKeys
    {
        //InscriptionTransfer:{transactionHash}
        public const string InscriptionTransferHashKeyFormat = "it:{0}";

        ////InscriptionTransfer:Block:{blockNumber}
        public const string InscriptionTransferByBlockKeyFormat = "it:b:{0}";

        ////InscriptionTransfer:{inscriptionType}:{name}
        public const string InscriptionTransferByInscriptionTypeFormat = "it:{0}:{1}";

        //"Inscription:{0}"
        public const string InscriptionKeyFormat = "i:{0}";

        //"Output:{0}:{1}"
        public const string OutputKeyFormat = "o:{0}:{1}";

        //"Block:InscriptionTransfers:LastSync"
        public const string LastInscriptionTransfersBlockSyncFormat = "b:it:l";

        //Block:ReadModels:LastSync
        public const string LastReadModelsBlockSyncFormat = "b:rm:l";

        //"Token:{0}"
        public const string TokenInfoKeyFormat = "t:{0}";

        //UserBalance:{shortInscriptionType}:{address}"
        public const string UserBalanceKeyFormat = "ub:{0}:{1}";

        //TokenBalance:{tick}"
        public const string TokenBalanceKeyFormat = "tb:{0}";

        //BalanceDetail:{address}:{tick}"
        public const string BalanceDetailKeyFormat = "bd:{0}:{1}";

        //TokenList
        public const string TokenListKeyFormat = "tl";

        public static string GetInscriptionTransferHashKey(uint256 transactionHash)
        {
            return string.Format(InscriptionTransferHashKeyFormat, transactionHash);
        }

        public static string GetInscriptionTransferByBlockKey(ulong blockNumber)
        {
            return string.Format(InscriptionTransferByBlockKeyFormat, blockNumber);
        }

        public static string GetInscriptionTransferByInscriptionTypeKey(InscriptionTypeEnum inscriptionType, string name)
        {
            var shortInscriptionType = GetShortInscriptionType(inscriptionType);
            return string.Format(InscriptionTransferByInscriptionTypeFormat, shortInscriptionType, name);
        }

        public static string GetInscriptionKey(string inscriptionId)
        {
            return string.Format(InscriptionKeyFormat, inscriptionId);
        }

        public static string GetOutputKey(string transactionHash, uint index)
        {
            return string.Format(OutputKeyFormat, transactionHash, index);
        }

        public static string GetLastInscriptionTransfersBlockSyncKey()
        {
            return LastInscriptionTransfersBlockSyncFormat;
        }

        public static string GetLastReadModelsBlockSyncKey()
        {
            return LastReadModelsBlockSyncFormat;
        }

        public static string GetTokenInfoKey(string tick)
        {
            return string.Format(TokenInfoKeyFormat, tick);
        }

        public static string GetUserBalanceKey(InscriptionTypeEnum inscriptionType, string address)
        {
            var shortInscriptionType = GetShortInscriptionType(inscriptionType);

            return string.Format(UserBalanceKeyFormat, shortInscriptionType, address);
        }

        public static string GetTokenBalanceKey(string tick)
        {
            return string.Format(TokenBalanceKeyFormat, tick);
        }

        public static string GetBalanceDetailKey(string address, string tick)
        {
            return string.Format(BalanceDetailKeyFormat, address, tick);
        }

        public static string GetTokenListKey()
        {
            return TokenListKeyFormat;
        }

        private static string GetShortInscriptionType(InscriptionTypeEnum inscriptionTypeEnum)
        {
            return inscriptionTypeEnum switch
            {
                InscriptionTypeEnum.Dns => "d",
                InscriptionTypeEnum.Nft => "n",
                InscriptionTypeEnum.Dogemap => "m",
                InscriptionTypeEnum.Token => "t",
                _ => throw new ArgumentException("Invalid string value for inscription type", nameof(InscriptionTransferType))
            };
        }
    }
}

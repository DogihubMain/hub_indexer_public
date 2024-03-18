using DogiHubIndexer.Entities.ReadModels;
using NBitcoin;
using System.Globalization;

namespace DogiHubIndexer.Entities.RawData.Extensions
{
    public static class InscriptionRawDataExtensions
    {
        public static InscriptionTransferRawData ToInscriptionTransfer(
            this InscriptionRawData inscriptionEntity,
            ulong blockNumber,
            uint inputIndex,
            string receiver,
            string? sender,
            uint256 transactionHash,
            int transactionIndex,
            DateTimeOffset transactionDate,
            InscriptionTransferType inscriptionTransferType
            ) => new InscriptionTransferRawData()
            {
                BlockNumber = blockNumber,
                InputIndex = inputIndex,
                Inscription = inscriptionEntity,
                InscriptionId = inscriptionEntity.Id.ToString(),
                Receiver = receiver,
                Sender = sender,
                TransactionHash = transactionHash,
                TransactionIndex = transactionIndex,
                InscriptionTransferType = inscriptionTransferType,
                Date = transactionDate
            };

        //The InscriptionTransferType.TRANSFER is never set here but in the algorithm that compare UTXO containing inscription 
        public static InscriptionTransferType ToInscriptionTransferType(
            this InscriptionRawData inscriptionEntity)
        {
            if (inscriptionEntity.Type == InscriptionTypeEnum.Token)
            {
                return inscriptionEntity.TokenContent!.op switch
                {
                    "deploy" => InscriptionTransferType.DEPLOY,
                    "mint" => InscriptionTransferType.MINT,
                    "transfer" => InscriptionTransferType.INSCRIBE_TRANSFER,
                    _ => throw new ArgumentException("Invalid string value for inscription type", nameof(InscriptionTransferType))
                };
            }
            else if (inscriptionEntity.Type == InscriptionTypeEnum.Dns)
            {
                return InscriptionTransferType.PENDING_DNS;
            }
            else if (inscriptionEntity.Type == InscriptionTypeEnum.Dogemap)
            {
                return InscriptionTransferType.PENDING_DOGEMAP;
            }
            else if (inscriptionEntity.Type == InscriptionTypeEnum.Nft)
            {
                return InscriptionTransferType.PENDING_NFT;
            }
            else throw new ArgumentException("Invalid string value for inscription type", nameof(InscriptionTransferType));
        }
    }
}

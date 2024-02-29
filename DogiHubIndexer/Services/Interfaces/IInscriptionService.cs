using DogiHubIndexer.Entities;
using DogiHubIndexer.Entities.RawData;
using NBitcoin;

namespace DogiHubIndexer.Services.Interfaces
{
    public interface IInscriptionService
    {
        InscriptionRawData? ExtractInscriptionFromScriptSig(
            ulong blockNumber,
            Block block,
            Transaction tx,
            TxIn txIn,
            uint256 genesisTxId,
            InscriptionId inscriptionId);

        List<Transaction> GetNftTransactionIdsInCurrentBlock(
            Block block,
            Transaction tx,
            TxIn txIn,
            out bool isComplete,
            bool isGenesis = false);
    }
}

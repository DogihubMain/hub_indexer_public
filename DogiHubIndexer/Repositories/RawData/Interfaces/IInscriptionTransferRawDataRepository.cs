using DogiHubIndexer.Entities.RawData;
using NBitcoin;

namespace DogiHubIndexer.Repositories.RawData.Interfaces
{
    public interface IInscriptionTransferRawDataRepository : IRawdataRepository
    {
        Task AddAsync(InscriptionTransferRawData entity, bool usePending);

        Task<List<InscriptionTransferRawData>> GetInscriptionTransfersByBlockAsync(ulong blockNumber);

        Task DeleteAsync(uint256 transactionHash, ulong blockNumber, InscriptionRawData inscriptionRawData);
    }
}
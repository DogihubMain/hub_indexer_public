using DogiHubIndexer.Entities.RawData;

namespace DogiHubIndexer.Repositories.RawData.Interfaces
{
    public interface IInscriptionTransferRawDataRepository : IRawdataRepository
    {
        Task AddAsync(InscriptionTransferRawData entity, bool usePending);

        Task<List<InscriptionTransferRawData>> GetInscriptionTransfersByBlockAsync(ulong blockNumber);

        Task DeleteAsync(ulong blockNumber, int transactionIndex, InscriptionRawData inscriptionRawData);
    }
}
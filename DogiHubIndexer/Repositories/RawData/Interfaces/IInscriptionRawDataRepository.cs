using DogiHubIndexer.Entities.RawData;

namespace DogiHubIndexer.Repositories.RawData.Interfaces
{
    public interface IInscriptionRawDataRepository : IRawdataRepository
    {
        Task AddAsync(InscriptionRawData entity);
        Task UpdateAsync(InscriptionRawData rawData);
        Task<InscriptionRawData?> GetAsync(string inscriptionId);
    }
}
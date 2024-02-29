namespace DogiHubIndexer.Repositories.RawData.Interfaces
{
    public interface IBlockRawDataRepository : IRawdataRepository
    {
        Task<bool> SetLastInscriptionTransfersBlockSync(ulong blockNumber);
        Task<bool> SetLastReadModelsBlockSync(ulong blockNumber);
        Task<ulong?> GetLastInscriptionTransfersBlockSync();
        Task<ulong?> GetLastReadModelsBlockSync();
    }
}

using DogiHubIndexer.Entities.RawData;
using NBitcoin;

namespace DogiHubIndexer.Repositories.RawData.Interfaces
{
    public interface IOutputRawDataRepository : IRawdataRepository
    {
        Task AddAsync(string transactionHash, uint index, OutputRawData outputEntity);
        Task DeleteAsync(uint256 transactionHash, uint index);
        Task<OutputRawData?> GetAsync(uint256 transactionHash, uint index);
    }
}
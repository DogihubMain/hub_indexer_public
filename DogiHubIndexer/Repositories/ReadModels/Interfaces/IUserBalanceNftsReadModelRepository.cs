using DogiHubIndexer.Entities.ReadModels;

namespace DogiHubIndexer.Repositories.ReadModels.Interfaces
{
    public interface IUserBalanceNftsReadModelRepository
    {
        Task AddAsync(UserBalanceNftReadModel userBalanceNftReadModel);
        Task DeleteAsync(string inscriptionId, string address);
        Task<UserBalanceNftReadModel?> GetAsync(string inscriptionId, string address);
        Task UpdateAsync(UserBalanceNftReadModel userBalanceNftReadModel);
    }
}

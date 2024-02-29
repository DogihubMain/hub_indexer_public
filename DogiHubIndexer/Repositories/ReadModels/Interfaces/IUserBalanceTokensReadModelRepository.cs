using DogiHubIndexer.Entities.ReadModels;

namespace DogiHubIndexer.Repositories.ReadModels.Interfaces
{
    public interface IUserBalanceTokensReadModelRepository
    {
        Task AddAsync(UserBalanceTokenReadModel userBalanceReadModel);
        Task<UserBalanceTokenReadModel?> GetAsync(string tick, string address);
        Task UpdateAsync(UserBalanceTokenReadModel userBalanceReadModel);
    }
}

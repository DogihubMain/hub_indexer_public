using DogiHubIndexer.Entities.ReadModels;

namespace DogiHubIndexer.Repositories.ReadModels.Interfaces
{
    public interface IUserBalanceDogemapsReadModelRepository
    {
        Task AddAsync(UserBalanceDogemapReadModel userBalanceDogemapReadModel);
        Task<UserBalanceDogemapReadModel?> GetAsync(int number, string address);
        Task DeleteAsync(int number, string address);
        Task UpdateAsync(UserBalanceDogemapReadModel userBalanceDogemapReadModel);
    }
}

using DogiHubIndexer.Entities.ReadModels;

namespace DogiHubIndexer.Repositories.ReadModels.Interfaces
{
    public interface IUserBalanceDnsReadModelRepository
    {
        Task AddAsync(UserBalanceDnsReadModel userBalanceDnsReadModel);
        Task<UserBalanceDnsReadModel?> GetAsync(string name, string address);
        Task DeleteAsync(string name, string address);
        Task UpdateAsync(UserBalanceDnsReadModel userBalanceDnsReadModel);
    }
}

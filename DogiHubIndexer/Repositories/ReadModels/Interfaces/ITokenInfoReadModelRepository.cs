using DogiHubIndexer.Entities.ReadModels;

namespace DogiHubIndexer.Repositories.ReadModels.Interfaces
{
    public interface ITokenInfoReadModelRepository
    {
        Task AddAsync(TokenReadModel tokenReadModel);
        Task<TokenReadModel?> GetAsync(string tick);
        Task UpdateAsync(TokenReadModel tokenReadModel);
    }
}

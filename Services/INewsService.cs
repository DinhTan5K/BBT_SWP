using start.Models;

namespace start.Services
{
    public interface INewsService
    {
        Task<IEnumerable<News>> GetAllAsync();
        Task<News?> GetByIdAsync(int id);
        Task AddAsync(News news);
        Task UpdateAsync(News news);
        Task DeleteAsync(int id);
    }
}
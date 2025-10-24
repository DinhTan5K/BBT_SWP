using start.Models;

public interface IStoreService
{
    Task<List<Branch>> GetAllBranchesAsync();
    Task<List<Branch>> FilterBranchesAsync(string search, string region);
    Task<List<string>> SuggestBranchNamesAsync(string term);
}
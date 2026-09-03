using PersonalDigitalVault.Api.DTOs.Search;
namespace PersonalDigitalVault.Api.Interfaces.Services;
public interface ISearchService 
{ 
    Task<List<SearchResultDto>> SearchAsync(string keyword);
}

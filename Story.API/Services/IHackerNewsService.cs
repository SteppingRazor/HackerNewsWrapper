using HackerNewsWrapper.Story.API.Model;

namespace HackerNewsWrapper.Story.API.Services;

public interface IHackerNewsService
{
    Task<IEnumerable<int>?> GetBestStoryIdsAsync(CancellationToken cancellationToken = default);
    Task<HackerNewsStory?> GetStoryAsync(int id, CancellationToken cancellationToken = default);
}
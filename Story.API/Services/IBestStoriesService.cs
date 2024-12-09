using HackerNewsWrapper.Story.API.Model;

namespace HackerNewsWrapper.Story.API.Services;
public interface IBestStoriesService
{
    Task<IEnumerable<StoryItem>?> GetBestStories(int n, CancellationToken cancellationToken = default);
}
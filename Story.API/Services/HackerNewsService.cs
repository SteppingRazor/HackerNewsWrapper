using HackerNewsWrapper.Story.API.Model;

namespace HackerNewsWrapper.Story.API.Services;

public class HackerNewsService : IHackerNewsService
{
    private readonly HttpClient _client;

    public HackerNewsService(HttpClient client)
    {
        _client = client;
    }

    public async Task<IEnumerable<int>?> GetBestStoryIdsAsync(CancellationToken cancellationToken = default)
    {
        var bestStoryIds = await _client.GetFromJsonAsync<IEnumerable<int>>(
            "beststories.json", cancellationToken);

        return bestStoryIds;
    }

    public async Task<HackerNewsStory?> GetStoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var story = await _client.GetFromJsonAsync<HackerNewsStory>(
            $"item/{id}.json", cancellationToken);

        return story;
    }
}

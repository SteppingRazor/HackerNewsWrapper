using HackerNewsWrapper.Story.API.Model;

namespace HackerNewsWrapper.Story.API.Services
{
    public class BestStoriesService : IBestStoriesService
    {
        private readonly IHackerNewsService _hackerNewsService;
        private readonly ICacheService _cacheService;

        public BestStoriesService(IHackerNewsService hackerNewsService, ICacheService cacheService)
        {
            _hackerNewsService = hackerNewsService;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<StoryItem>?> GetBestStories(int n, CancellationToken cancellationToken = default)
        {
            // Get processed best stories from cache
            var processedCacheKey = $"processed_best_stories_{n}";
            var cachedProcessedStories = await _cacheService.GetAsync<IEnumerable<StoryItem>?>(processedCacheKey);

            if (cachedProcessedStories != null)
            {
                return cachedProcessedStories;
            }

            // Get top n story ids
            var topStoryIds = await GetTopStoryIds(n, cancellationToken);
            if (topStoryIds == null || !topStoryIds.Any())
            {
                return null;
            }

            // Fetch content of top stories
            var stories = await FetchStoriesContent(topStoryIds, cancellationToken);

            // Sort and map
            var sortedStories = stories
                .Where(story => story != null)
                .OrderByDescending(story => story!.Score)
                .Select(story => new StoryItem()
                {
                    Title = story!.Title,
                    Uri = story.Url,
                    PostedBy = story.By,
                    Time = DateTimeOffset.FromUnixTimeSeconds(story.Time).ToString("yyyy-MM-ddTHH:mm:sszzz"),
                    Score = story.Score,
                    CommentCount = story.Descendants
                });

            // Save processed stories to cache
            await _cacheService.SetAsync<IEnumerable<StoryItem>?>(processedCacheKey, sortedStories);

            return sortedStories;
        }

        private async Task<IEnumerable<int>?> GetTopStoryIds(int n, CancellationToken cancellationToken = default)
        {
            var cacheKey = "bestStoryIds";
            var bestStoryIds = await _cacheService.GetAsync<IEnumerable<int>>(cacheKey);

            if (bestStoryIds == null)
            {
                bestStoryIds = await _hackerNewsService.GetBestStoryIdsAsync(cancellationToken);

                if (bestStoryIds == null || !bestStoryIds.Any())
                {
                    return null;
                }

                await _cacheService.SetAsync(cacheKey, bestStoryIds);
            }

            // Limit to top 'n' stories
            return bestStoryIds.Take(n);
        }

        private async Task<IEnumerable<HackerNewsStory?>> FetchStoriesContent(IEnumerable<int> storyIds, CancellationToken cancellationToken = default)
        {
            var storyTasks = storyIds.Select(async id =>
            {
                var storyCacheKey = $"story_{id}";
                var story = await _cacheService.GetAsync<HackerNewsStory>(storyCacheKey);

                if (story == null)
                {
                    story = await _hackerNewsService.GetStoryAsync(id, cancellationToken);

                    if (story != null)
                    {
                        await _cacheService.SetAsync(storyCacheKey, story);
                    }
                }

                return story;
            });

            return (await Task.WhenAll(storyTasks))
                .ToList();
        }
    }
}

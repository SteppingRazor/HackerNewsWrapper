using Bogus;
using HackerNewsWrapper.Story.API.Services;
using HackerNewsWrapper.Story.API.Model;
using Moq;

namespace Story.UnitTests;

public class BestStoriesServiceTests
{
    private readonly Mock<IHackerNewsService> _hackerNewsServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly BestStoriesService _bestStoriesService;

    public BestStoriesServiceTests()
    {
        _hackerNewsServiceMock = new Mock<IHackerNewsService>();
        _cacheServiceMock = new Mock<ICacheService>();
        _bestStoriesService = new BestStoriesService(_hackerNewsServiceMock.Object, _cacheServiceMock.Object);
    }

    private static IEnumerable<StoryItem> GenerateFakeStories(int count)
    {
        var faker = new Faker<StoryItem>()
            .RuleFor(s => s.Title, f => f.Lorem.Sentence())
            .RuleFor(s => s.Score, f => f.Random.Int(1, 500))
            .RuleFor(s => s.PostedBy, f => f.Internet.UserName())
            .RuleFor(s => s.Time, f => f.Date.Recent().ToString("yyyy-MM-ddTHH:mm:sszzz"))
            .RuleFor(s => s.Uri, f => f.Internet.Url())
            .RuleFor(s => s.CommentCount, f => f.Random.Int(0, 1000));

        return faker.Generate(count);
    }

    [Fact]
    public async Task GetBestStories_ReturnsCachedStories_IfAvailable()
    {
        // Arrange
        var cachedStories = GenerateFakeStories(5).ToList();

        _cacheServiceMock.Setup(c => c.GetAsync<IEnumerable<StoryItem>?>(It.IsAny<string>()))
            .ReturnsAsync(cachedStories);

        // Act
        var result = await _bestStoriesService.GetBestStories(5);

        // Assert
        Assert.Equal(cachedStories, result);
        _cacheServiceMock.Verify(c => c.GetAsync<IEnumerable<StoryItem>?>(It.IsAny<string>()), Times.Once);
        _hackerNewsServiceMock.Verify(h => h.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetBestStories_FetchesStories_IfNotCached()
    {
        // Arrange
        var storyIds = new List<int> { 1, 2, 3 };
        var stories = GenerateFakeStories(3).ToList();

        _cacheServiceMock.SetupSequence(c => c.GetAsync<IEnumerable<StoryItem>?>(It.IsAny<string>()))
            .ReturnsAsync((IEnumerable<StoryItem>?)null);

        _cacheServiceMock.SetupSequence(c => c.GetAsync<IEnumerable<int>?>("bestStoryIds"))
            .ReturnsAsync((IEnumerable<int>?)null);

        _hackerNewsServiceMock.Setup(h => h.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(storyIds);

        _hackerNewsServiceMock.Setup(h => h.GetStoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>((id, token) => Task.FromResult<HackerNewsStory?>(new HackerNewsStory
            {
                Title = stories.First().Title,
                Score = stories.First().Score,
                By = stories.First().PostedBy,
                Time = (int)new DateTimeOffset(DateTime.Parse(stories.First().Time)).ToUnixTimeSeconds(),
                Url = stories.First().Uri,
                Descendants = stories.First().CommentCount
            }));

        // Act
        var result = await _bestStoriesService.GetBestStories(3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        _cacheServiceMock.Verify(c => c.SetAsync<IEnumerable<StoryItem>?>(It.IsAny<string>(), It.IsAny<IEnumerable<StoryItem>>()), Times.Once);
    }

    [Fact]
    public async Task GetBestStories_CachesFetchedStories()
    {
        // Arrange
        var storyIds = new List<int> { 1, 2 };
        var stories = GenerateFakeStories(2).ToList();

        _cacheServiceMock.Setup(c => c.GetAsync<IEnumerable<StoryItem>?>(It.IsAny<string>()))
            .ReturnsAsync((IEnumerable<StoryItem>?)null);

        _cacheServiceMock.Setup(c => c.GetAsync<IEnumerable<int>>(It.IsAny<string>()))
            .ReturnsAsync(storyIds);

        _hackerNewsServiceMock.Setup(h => h.GetStoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>((id, token) => Task.FromResult<HackerNewsStory?>(new HackerNewsStory
            {
                Title = stories.First().Title,
                Score = stories.First().Score,
                By = stories.First().PostedBy,
                Time = (int)new DateTimeOffset(DateTime.Parse(stories.First().Time)).ToUnixTimeSeconds(),
                Url = stories.First().Uri,
                Descendants = stories.First().CommentCount
            }));

        // Act
        await _bestStoriesService.GetBestStories(2);

        // Assert
        _cacheServiceMock.Verify(c => c.SetAsync<IEnumerable<StoryItem>?>(It.IsAny<string>(), It.IsAny<IEnumerable<StoryItem>>()), Times.Once);
    }

    [Fact]
    public async Task GetBestStories_CollectionIsSortedDescendingByScore()
    {
        // Arrange
        var stories = GenerateFakeStories(5).ToList();
        var storyIds = stories.Select((s, index) => index).ToList();

        _cacheServiceMock.Setup(c => c.GetAsync<IEnumerable<StoryItem>?>(It.IsAny<string>()))
            .ReturnsAsync((IEnumerable<StoryItem>?)null);

        _cacheServiceMock.Setup(c => c.GetAsync<IEnumerable<int>>(It.IsAny<string>()))
            .ReturnsAsync(storyIds);

        _hackerNewsServiceMock.Setup(h => h.GetStoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>((id, token) => Task.FromResult<HackerNewsStory?>(new HackerNewsStory
            {
                Title = stories[id].Title,
                Score = stories[id].Score,
                By = stories[id].PostedBy,
                Time = (int)new DateTimeOffset(DateTime.Parse(stories[id].Time)).ToUnixTimeSeconds(),
                Url = stories[id].Uri,
                Descendants = stories[id].CommentCount
            }));

        // Act
        var result = await _bestStoriesService.GetBestStories(5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Count());
        Assert.True(result.Zip(result.Skip(1), (a, b) => a.Score >= b.Score).All(x => x));
    }
}

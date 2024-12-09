using Asp.Versioning;
using HackerNewsWrapper.Story.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace HackerNewsWrapper.Story.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class BestStoriesController : ControllerBase
{
    private readonly IBestStoriesService _bestStoriesService;

    public BestStoriesController(IBestStoriesService bestStoriesService)
    {
        _bestStoriesService = bestStoriesService;
    }

    /// <summary>
    /// Request to fetch best stories sorted by score
    /// </summary>
    /// <param name="n">Number of best stories to fetch (integer, required, must be greater than 0).</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetBestStories([FromQuery] int n, CancellationToken cancellationToken)
    {
        try
        {
            if (n <= 0)
            {
                return Problem(detail: $"Parameter {nameof(n)} must be greater than 0.", statusCode: StatusCodes.Status400BadRequest);
            }

            var bestStories = await _bestStoriesService.GetBestStories(n, cancellationToken);

            if (bestStories == null || !bestStories.Any())
            {
                return Problem(detail: $"No best stories found.", statusCode: StatusCodes.Status404NotFound);
            }

            return Ok(bestStories);
        }
        catch (Exception ex)
        {
            return Problem($"An error occurred: {ex.Message}");
        }
    }
}

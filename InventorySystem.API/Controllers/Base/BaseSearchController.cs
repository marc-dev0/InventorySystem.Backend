using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.API.Controllers.Base;

/// <summary>
/// Base controller that provides common search functionality
/// </summary>
/// <typeparam name="TDto">The DTO type for search results</typeparam>
[ApiController]
public abstract class BaseSearchController<TDto> : ControllerBase
{
    /// <summary>
    /// Searches entities based on a search term
    /// </summary>
    /// <param name="term">The search term</param>
    /// <returns>List of entities matching the search term</returns>
    [HttpGet("search")]
    public virtual async Task<ActionResult<IEnumerable<TDto>>> Search([FromQuery] string term)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term))
                return BadRequest("Search term is required");

            var results = await SearchItemsAsync(term);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Abstract method that derived controllers must implement to perform the actual search
    /// </summary>
    /// <param name="term">The search term</param>
    /// <returns>Collection of entities matching the search term</returns>
    protected abstract Task<IEnumerable<TDto>> SearchItemsAsync(string term);
}
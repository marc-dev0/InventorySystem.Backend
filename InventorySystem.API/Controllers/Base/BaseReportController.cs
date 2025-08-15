using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.API.Controllers.Base;

/// <summary>
/// Base controller that provides common report functionality
/// </summary>
/// <typeparam name="TDto">The DTO type for entities</typeparam>
[ApiController]
public abstract class BaseReportController<TDto> : ControllerBase
{
    /// <summary>
    /// Gets entities by date range
    /// </summary>
    /// <param name="startDate">Start date for the range</param>
    /// <param name="endDate">End date for the range</param>
    /// <returns>List of entities within the date range</returns>
    [HttpGet("date-range")]
    public virtual async Task<ActionResult<IEnumerable<TDto>>> GetByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            if (startDate > endDate)
                return BadRequest("Start date cannot be greater than end date");

            var items = await GetItemsByDateRangeAsync(startDate, endDate);
            return Ok(items);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a report for the specified date range
    /// </summary>
    /// <param name="startDate">Start date for the report</param>
    /// <param name="endDate">End date for the report</param>
    /// <returns>Report data for the specified date range</returns>
    [HttpGet("reports")]
    public virtual async Task<ActionResult<object>> GetReport(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            if (startDate > endDate)
                return BadRequest("Start date cannot be greater than end date");

            var report = await GetReportAsync(startDate, endDate);
            return Ok(report);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Abstract method that derived controllers must implement to get items by date range
    /// </summary>
    /// <param name="startDate">Start date for the range</param>
    /// <param name="endDate">End date for the range</param>
    /// <returns>Collection of entities within the date range</returns>
    protected abstract Task<IEnumerable<TDto>> GetItemsByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Abstract method that derived controllers must implement to generate reports
    /// </summary>
    /// <param name="startDate">Start date for the report</param>
    /// <param name="endDate">End date for the report</param>
    /// <returns>Report data</returns>
    protected abstract Task<object> GetReportAsync(DateTime startDate, DateTime endDate);
}
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.API.Controllers.Base;

/// <summary>
/// Base controller that provides common CRUD operations
/// </summary>
/// <typeparam name="TDto">The DTO type for read operations</typeparam>
/// <typeparam name="TCreateDto">The DTO type for create operations</typeparam>
/// <typeparam name="TUpdateDto">The DTO type for update operations</typeparam>
[ApiController]
[Route("api/[controller]")]
public abstract class BaseCrudController<TDto, TCreateDto, TUpdateDto> : ControllerBase
{
    /// <summary>
    /// Gets all entities
    /// </summary>
    /// <returns>List of all entities</returns>
    [HttpGet]
    public virtual async Task<ActionResult<IEnumerable<TDto>>> GetAll()
    {
        try
        {
            var items = await GetAllItemsAsync();
            return Ok(items);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets an entity by ID
    /// </summary>
    /// <param name="id">The entity ID</param>
    /// <returns>The entity if found, NotFound otherwise</returns>
    [HttpGet("{id}")]
    public virtual async Task<ActionResult<TDto>> GetById(int id)
    {
        try
        {
            var item = await GetItemByIdAsync(id);
            if (item == null)
                return NotFound();
            return Ok(item);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all active entities
    /// </summary>
    /// <returns>List of active entities</returns>
    [HttpGet("active")]
    public virtual async Task<ActionResult<IEnumerable<TDto>>> GetActive()
    {
        try
        {
            var items = await GetActiveItemsAsync();
            return Ok(items);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a new entity
    /// </summary>
    /// <param name="createDto">The data for creating the entity</param>
    /// <returns>The created entity</returns>
    [HttpPost]
    public virtual async Task<ActionResult<TDto>> Create(TCreateDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var createdItem = await CreateItemAsync(createDto);
            return CreatedAtAction(nameof(GetById), new { id = GetItemId(createdItem) }, createdItem);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing entity
    /// </summary>
    /// <param name="id">The entity ID</param>
    /// <param name="updateDto">The data for updating the entity</param>
    /// <returns>NoContent if successful</returns>
    [HttpPut("{id}")]
    public virtual async Task<IActionResult> Update(int id, TUpdateDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await UpdateItemAsync(id, updateDto);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes an entity
    /// </summary>
    /// <param name="id">The entity ID</param>
    /// <returns>NoContent if successful</returns>
    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(int id)
    {
        try
        {
            await DeleteItemAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    // Abstract methods that derived controllers must implement
    protected abstract Task<IEnumerable<TDto>> GetAllItemsAsync();
    protected abstract Task<TDto?> GetItemByIdAsync(int id);
    protected abstract Task<IEnumerable<TDto>> GetActiveItemsAsync();
    protected abstract Task<TDto> CreateItemAsync(TCreateDto createDto);
    protected abstract Task UpdateItemAsync(int id, TUpdateDto updateDto);
    protected abstract Task DeleteItemAsync(int id);
    protected abstract int GetItemId(TDto item);
}
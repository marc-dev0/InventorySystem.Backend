using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventorySystem.Core.Interfaces;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoresController : ControllerBase
{
    private readonly IStoreRepository _storeRepository;
    private readonly IProductStockRepository _productStockRepository;
    private readonly ILogger<StoresController> _logger;

    public StoresController(IStoreRepository storeRepository, IProductStockRepository productStockRepository, ILogger<StoresController> logger)
    {
        _storeRepository = storeRepository;
        _productStockRepository = productStockRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all active stores with initial stock status
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetStores()
    {
        try
        {
            _logger.LogInformation("Getting all stores...");
            var stores = await _storeRepository.GetAllAsync();
            _logger.LogInformation($"Found {stores.Count()} total stores in database");
            
            var activeStores = stores.Where(s => s.Active && !s.IsDeleted).ToList();
            _logger.LogInformation($"Found {activeStores.Count} active stores after filtering");
            
            var result = new List<object>();
            
            foreach (var store in activeStores.OrderBy(s => s.Name))
            {
                _logger.LogInformation($"Processing store: Id={store.Id}, Code={store.Code}, Name={store.Name}, Active={store.Active}, IsDeleted={store.IsDeleted}");
                
                result.Add(new
                {
                    Id = store.Id,
                    Code = store.Code,
                    Name = store.Name,
                    Address = store.Address,
                    Phone = store.Phone,
                    HasInitialStock = store.HasInitialStock // Use the actual field from Store entity
                });
            }

            _logger.LogInformation($"Returning {result.Count} stores to client");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stores");
            return StatusCode(500, "Error interno del servidor");
        }
    }

    /// <summary>
    /// Get store by code
    /// </summary>
    [HttpGet("{code}")]
    public async Task<ActionResult<object>> GetStoreByCode(string code)
    {
        try
        {
            var stores = await _storeRepository.GetAllAsync();
            var store = stores.FirstOrDefault(s => s.Code == code && s.Active && !s.IsDeleted);
            
            if (store == null)
                return NotFound();

            var result = new
            {
                Id = store.Id,
                Code = store.Code,
                Name = store.Name,
                Address = store.Address,
                Phone = store.Phone
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting store by code {Code}", code);
            return StatusCode(500, "Error interno del servidor");
        }
    }
}
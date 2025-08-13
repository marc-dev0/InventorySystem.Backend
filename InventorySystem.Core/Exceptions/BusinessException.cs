namespace InventorySystem.Core.Exceptions;

public class BusinessException : Exception
{
    public string ErrorCode { get; }
    
    public BusinessException(string message) : base(message)
    {
        ErrorCode = "BUSINESS_ERROR";
    }
    
    public BusinessException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }
    
    public BusinessException(string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = "BUSINESS_ERROR";
    }
}

public class EntityNotFoundException : BusinessException
{
    public EntityNotFoundException(string entityName, object id) 
        : base($"{entityName} with ID {id} was not found", "ENTITY_NOT_FOUND")
    {
    }
}

public class DuplicateEntityException : BusinessException
{
    public DuplicateEntityException(string entityName, string field, object value)
        : base($"{entityName} with {field} '{value}' already exists", "DUPLICATE_ENTITY")
    {
    }
}

public class InsufficientStockException : BusinessException
{
    public InsufficientStockException(string productName, int availableStock, int requestedQuantity)
        : base($"Insufficient stock for product '{productName}'. Available: {availableStock}, Requested: {requestedQuantity}", "INSUFFICIENT_STOCK")
    {
    }
}
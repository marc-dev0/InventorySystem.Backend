namespace InventorySystem.Core.Entities;

public class SystemConfiguration : BaseEntity
{
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public string ConfigType { get; set; } = "String"; // String, Json, Number, Boolean, etc.
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty; // Import, Email, System, etc.
    public bool Active { get; set; } = true;
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
namespace InventorySystem.Core.Entities;

public enum MovementType
{
    // Stock increases
    StockIn = 1,
    Purchase = 2,
    Adjustment_In = 3,
    Return_In = 4,
    Transfer_In = 5,
    
    // Stock decreases  
    StockOut = 10,
    Sale = 11,
    Adjustment_Out = 12,
    Damage = 13,
    Loss = 14,
    Theft = 15,
    Expired = 16,
    Transfer_Out = 17,
    
    // From Tandia import
    TandiaImport_Sale = 20,
    TandiaImport_Adjustment = 21,
    
    // Manual adjustments
    ManualCorrection = 30
}

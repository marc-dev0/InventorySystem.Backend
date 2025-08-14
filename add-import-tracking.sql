-- Script para agregar campos de tracking de importaciones manualmente

-- Agregar columnas a la tabla Sales
ALTER TABLE "Sales" 
ADD COLUMN IF NOT EXISTS "ImportedAt" timestamp with time zone,
ADD COLUMN IF NOT EXISTS "ImportSource" text,
ADD COLUMN IF NOT EXISTS "ImportBatchId" integer;

-- Agregar columnas a la tabla Products  
ALTER TABLE "Products"
ADD COLUMN IF NOT EXISTS "ImportBatchId" integer;

-- Agregar columnas a la tabla ProductStocks
ALTER TABLE "ProductStocks"
ADD COLUMN IF NOT EXISTS "ImportBatchId" integer;

-- Agregar columna BatchCode a ImportBatches si ya existe
ALTER TABLE "ImportBatches" 
ADD COLUMN IF NOT EXISTS "BatchCode" varchar(100) UNIQUE;

-- Crear tabla ImportBatches
CREATE TABLE IF NOT EXISTS "ImportBatches" (
    "Id" serial PRIMARY KEY,
    "BatchCode" varchar(100) NOT NULL UNIQUE,
    "BatchType" varchar(50) NOT NULL,
    "FileName" varchar(255) NOT NULL,
    "StoreCode" varchar(10),
    "TotalRecords" integer NOT NULL DEFAULT 0,
    "SuccessCount" integer NOT NULL DEFAULT 0,
    "SkippedCount" integer NOT NULL DEFAULT 0,
    "ErrorCount" integer NOT NULL DEFAULT 0,
    "Errors" text,
    "Warnings" text,
    "ImportDate" timestamp with time zone NOT NULL,
    "ImportedBy" varchar(100) NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" varchar(100),
    "DeleteReason" varchar(500),
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL DEFAULT false
);

-- Agregar foreign keys (opcional, pueden fallar si hay datos inconsistentes)
DO $$
BEGIN
    -- Foreign key de Sales a ImportBatches
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'FK_Sales_ImportBatches_ImportBatchId'
    ) THEN
        ALTER TABLE "Sales" 
        ADD CONSTRAINT "FK_Sales_ImportBatches_ImportBatchId" 
        FOREIGN KEY ("ImportBatchId") REFERENCES "ImportBatches"("Id") ON DELETE SET NULL;
    END IF;
    
    -- Foreign key de Products a ImportBatches  
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'FK_Products_ImportBatches_ImportBatchId'
    ) THEN
        ALTER TABLE "Products" 
        ADD CONSTRAINT "FK_Products_ImportBatches_ImportBatchId" 
        FOREIGN KEY ("ImportBatchId") REFERENCES "ImportBatches"("Id") ON DELETE SET NULL;
    END IF;
    
    -- Foreign key de ProductStocks a ImportBatches
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'FK_ProductStocks_ImportBatches_ImportBatchId'
    ) THEN
        ALTER TABLE "ProductStocks" 
        ADD CONSTRAINT "FK_ProductStocks_ImportBatches_ImportBatchId" 
        FOREIGN KEY ("ImportBatchId") REFERENCES "ImportBatches"("Id") ON DELETE SET NULL;
    END IF;
EXCEPTION 
    WHEN OTHERS THEN
        RAISE NOTICE 'Error adding foreign keys: %', SQLERRM;
END $$;

-- Verificar que las columnas se agregaron correctamente
SELECT 
    'Sales' as tabla,
    column_name,
    data_type
FROM information_schema.columns 
WHERE table_name = 'Sales' 
AND column_name IN ('ImportedAt', 'ImportSource', 'ImportBatchId')

UNION ALL

SELECT 
    'Products' as tabla,
    column_name,
    data_type
FROM information_schema.columns 
WHERE table_name = 'Products' 
AND column_name = 'ImportBatchId'

UNION ALL

SELECT 
    'ProductStocks' as tabla,
    column_name,
    data_type
FROM information_schema.columns 
WHERE table_name = 'ProductStocks' 
AND column_name = 'ImportBatchId'

UNION ALL

SELECT 
    'ImportBatches' as tabla,
    'table_exists' as column_name,
    'boolean' as data_type
FROM information_schema.tables 
WHERE table_name = 'ImportBatches';
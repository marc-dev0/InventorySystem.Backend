#!/bin/bash
# seed-data.sh - Load sample data into the inventory system

API_URL="http://localhost:5000"

echo "🌱 Loading seed data into Inventory Management System..."

# Function to make API calls with error handling
api_call() {
    local method=$1
    local endpoint=$2
    local data=$3
    
    response=$(curl -s -w "%{http_code}" -X $method "$API_URL$endpoint" \
        -H "Content-Type: application/json" \
        -d "$data")
    
    http_code="${response: -3}"
    body="${response%???}"
    
    if [[ $http_code -ge 200 && $http_code -lt 300 ]]; then
        echo "✅ $method $endpoint - Success ($http_code)"
        return 0
    else
        echo "❌ $method $endpoint - Failed ($http_code)"
        echo "Response: $body"
        return 1
    fi
}

# Wait for API to be ready
echo "Waiting for API to be ready..."
for i in {1..30}; do
    if curl -s "$API_URL/health" > /dev/null; then
        echo "✅ API is ready"
        break
    fi
    sleep 1
done

# Create categories
echo "📂 Creating categories..."
api_call POST "/api/categories" '{
    "name": "Electronics",
    "description": "Electronic devices and accessories"
}'

api_call POST "/api/categories" '{
    "name": "Clothing",
    "description": "Apparel and fashion items"
}'

api_call POST "/api/categories" '{
    "name": "Home & Garden",
    "description": "Home improvement and garden supplies"
}'

# Create suppliers
echo "🏭 Creating suppliers..."
api_call POST "/api/suppliers" '{
    "name": "TechSupply Inc.",
    "phone": "+1-555-0001",
    "email": "orders@techsupply.com",
    "address": "123 Technology Ave, Tech City, TC 12345"
}'

api_call POST "/api/suppliers" '{
    "name": "Fashion Wholesale",
    "phone": "+1-555-0002",
    "email": "sales@fashionwholesale.com",
    "address": "456 Fashion Blvd, Style City, SC 67890"
}'

# Create customers
echo "👥 Creating customers..."
api_call POST "/api/customers" '{
    "name": "John Doe",
    "phone": "+1-555-1001",
    "email": "john.doe@email.com",
    "address": "789 Customer St, Client City, CC 11111",
    "document": "ID123456789"
}'

api_call POST "/api/customers" '{
    "name": "Jane Smith",
    "phone": "+1-555-1002",
    "email": "jane.smith@email.com",
    "address": "321 Buyer Ave, Purchase City, PC 22222",
    "document": "ID987654321"
}'

# Create products
echo "📦 Creating products..."
api_call POST "/api/products" '{
    "code": "ELEC001",
    "name": "Smartphone Android",
    "description": "Latest Android smartphone with 128GB storage",
    "purchasePrice": 300.00,
    "salePrice": 450.00,
    "stock": 25,
    "minimumStock": 5,
    "unit": "unit",
    "categoryId": 1,
    "supplierId": 1
}'

api_call POST "/api/products" '{
    "code": "ELEC002",
    "name": "Wireless Headphones",
    "description": "Bluetooth wireless headphones with noise cancellation",
    "purchasePrice": 80.00,
    "salePrice": 120.00,
    "stock": 50,
    "minimumStock": 10,
    "unit": "unit",
    "categoryId": 1,
    "supplierId": 1
}'

api_call POST "/api/products" '{
    "code": "CLOTH001",
    "name": "Cotton T-Shirt",
    "description": "100% cotton t-shirt, various colors",
    "purchasePrice": 8.00,
    "salePrice": 15.00,
    "stock": 100,
    "minimumStock": 20,
    "unit": "unit",
    "categoryId": 2,
    "supplierId": 2
}'

api_call POST "/api/products" '{
    "code": "CLOTH002",
    "name": "Denim Jeans",
    "description": "Classic blue denim jeans, multiple sizes",
    "purchasePrice": 25.00,
    "salePrice": 45.00,
    "stock": 75,
    "minimumStock": 15,
    "unit": "unit",
    "categoryId": 2,
    "supplierId": 2
}'

api_call POST "/api/products" '{
    "code": "HOME001",
    "name": "Garden Hose",
    "description": "50ft garden hose with spray nozzle",
    "purchasePrice": 20.00,
    "salePrice": 35.00,
    "stock": 30,
    "minimumStock": 5,
    "unit": "unit",
    "categoryId": 3
}'

echo "✅ Seed data loading completed!"
echo ""
echo "📊 You can now:"
echo "   - View products: GET $API_URL/api/products"
echo "   - View categories: GET $API_URL/api/categories"
echo "   - Check low stock: GET $API_URL/api/products/low-stock"
echo "   - Access Swagger UI: $API_URL"

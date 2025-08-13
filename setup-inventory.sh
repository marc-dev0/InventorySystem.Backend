#!/bin/bash
# setup-inventory-project.sh - Complete setup script for Inventory Management System

echo "🚀 Setting up Inventory Management System..."
echo "================================================"

# Colors for better output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

# Check if .NET 9 is installed
check_dotnet() {
    print_info "Checking .NET Core installation..."
    if ! command -v dotnet &> /dev/null; then
        print_warning ".NET Core not found. Installing..."
        
        # Download and install .NET 9
        wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
        sudo dpkg -i packages-microsoft-prod.deb
        sudo apt-get update
        sudo apt-get install -y dotnet-sdk-9.0
        
        if command -v dotnet &> /dev/null; then
            print_status ".NET Core $(dotnet --version) installed successfully"
        else
            print_error "Failed to install .NET Core"
            exit 1
        fi
    else
        print_status ".NET Core $(dotnet --version) detected"
    fi
}

# Create project structure
create_project_structure() {
    print_info "Creating project structure..."
    
    # Create solution
    dotnet new sln -n InventorySystem
    
    # Create projects
    print_info "Creating API project..."
    dotnet new webapi -n InventorySystem.API
    
    print_info "Creating Core project..."
    dotnet new classlib -n InventorySystem.Core
    
    print_info "Creating Infrastructure project..."
    dotnet new classlib -n InventorySystem.Infrastructure
    
    print_info "Creating Application project..."
    dotnet new classlib -n InventorySystem.Application
    
    # Add projects to solution
    dotnet sln add InventorySystem.API/InventorySystem.API.csproj
    dotnet sln add InventorySystem.Core/InventorySystem.Core.csproj
    dotnet sln add InventorySystem.Infrastructure/InventorySystem.Infrastructure.csproj
    dotnet sln add InventorySystem.Application/InventorySystem.Application.csproj
    
    print_status "Project structure created successfully"
}

# Configure project references
configure_references() {
    print_info "Configuring project references..."
    
    # API references
    cd InventorySystem.API
    dotnet add reference ../InventorySystem.Application/InventorySystem.Application.csproj
    dotnet add reference ../InventorySystem.Infrastructure/InventorySystem.Infrastructure.csproj
    
    # Application references
    cd ../InventorySystem.Application
    dotnet add reference ../InventorySystem.Core/InventorySystem.Core.csproj
    
    # Infrastructure references
    cd ../InventorySystem.Infrastructure
    dotnet add reference ../InventorySystem.Core/InventorySystem.Core.csproj
    dotnet add reference ../InventorySystem.Application/InventorySystem.Application.csproj
    
    cd ..
    print_status "Project references configured"
}

# Install NuGet packages
install_packages() {
    print_info "Installing NuGet packages..."
    
    # API packages
    print_info "Installing API packages..."
    cd InventorySystem.API
    dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.0.0
    dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.0.0
    dotnet add package Swashbuckle.AspNetCore --version 7.0.0
    dotnet add package Serilog.AspNetCore --version 8.0.0
    dotnet add package Serilog.Sinks.Console --version 5.0.0
    dotnet add package Serilog.Sinks.File --version 5.0.0
    dotnet add package Microsoft.AspNetCore.Diagnostics.HealthChecks --version 2.2.0
    dotnet add package AspNetCore.HealthChecks.Npgsql --version 8.0.0
    
    # Infrastructure packages
    print_info "Installing Infrastructure packages..."
    cd ../InventorySystem.Infrastructure
    dotnet add package Microsoft.EntityFrameworkCore --version 9.0.0
    dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.0.0
    dotnet add package Microsoft.EntityFrameworkCore.Tools --version 9.0.0
    
    # Application packages
    print_info "Installing Application packages..."
    cd ../InventorySystem.Application
    dotnet add package AutoMapper --version 13.0.0
    dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection --version 13.0.0
    dotnet add package FluentValidation --version 11.8.0
    dotnet add package MediatR --version 12.2.0
    
    cd ..
    print_status "NuGet packages installed successfully"
}

# Setup PostgreSQL
setup_postgresql() {
    print_info "Checking PostgreSQL installation..."
    
    if command -v psql &> /dev/null; then
        print_status "PostgreSQL detected"
    else
        print_warning "PostgreSQL not found. Installing..."
        sudo apt update
        sudo apt install -y postgresql postgresql-contrib
        sudo systemctl start postgresql
        sudo systemctl enable postgresql
        print_status "PostgreSQL installed and started"
    fi
    
    # Create development database
    print_info "Setting up development database..."
    sudo -u postgres psql << EOF
CREATE USER inventory_user WITH PASSWORD 'inventory_pass';
CREATE DATABASE inventory_db OWNER inventory_user;
GRANT ALL PRIVILEGES ON DATABASE inventory_db TO inventory_user;
\q
EOF
    
    if [ $? -eq 0 ]; then
        print_status "Development database 'inventory_db' created successfully"
    else
        print_warning "Database might already exist or there was an issue creating it"
    fi
}

# Create configuration files
create_config_files() {
    print_info "Creating configuration files..."
    
    # Create appsettings.Development.json
    cat > InventorySystem.API/appsettings.Development.json << 'EOF'
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=inventory_db;Username=inventory_user;Password=inventory_pass"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information",
      "Microsoft.EntityFrameworkCore.Infrastructure": "Information",
      "InventorySystem": "Debug"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information",
        "System": "Warning"
      }
    }
  }
}
EOF

    # Create appsettings.json
    cat > InventorySystem.API/appsettings.json << 'EOF'
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=inventory_db;Username=postgres;Password=postgres"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information",
      "Microsoft.EntityFrameworkCore.Infrastructure": "Warning",
      "InventorySystem": "Debug"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/inventory-system-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      }
    ]
  },
  "AllowedHosts": "*"
}
EOF

    # Create Dockerfile
    cat > Dockerfile << 'EOF'
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
COPY ["InventorySystem.API/InventorySystem.API.csproj", "InventorySystem.API/"]
COPY ["InventorySystem.Application/InventorySystem.Application.csproj", "InventorySystem.Application/"]
COPY ["InventorySystem.Core/InventorySystem.Core.csproj", "InventorySystem.Core/"]
COPY ["InventorySystem.Infrastructure/InventorySystem.Infrastructure.csproj", "InventorySystem.Infrastructure/"]

RUN dotnet restore "InventorySystem.API/InventorySystem.API.csproj"

# Copy all source code and build
COPY . .
WORKDIR "/src/InventorySystem.API"
RUN dotnet build "InventorySystem.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "InventorySystem.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Railway requires the app to listen on the PORT environment variable
ENV ASPNETCORE_URLS=http://+:$PORT
ENTRYPOINT ["dotnet", "InventorySystem.API.dll"]
EOF

    # Create .dockerignore
    cat > .dockerignore << 'EOF'
**/.dockerignore
**/.env
**/.git
**/.gitignore
**/.project
**/.settings
**/.toolstarget
**/.vs
**/.vscode
**/*.*proj.user
**/*.dbmdl
**/*.jfm
**/azds.yaml
**/bin
**/charts
**/docker-compose*
**/Dockerfile*
**/node_modules
**/npm-debug.log
**/obj
**/secrets.dev.yaml
**/values.dev.yaml
LICENSE
README.md
logs/
EOF

    # Create railway.json for Railway deployment
    cat > railway.json << 'EOF'
{
  "build": {
    "builder": "DOCKERFILE",
    "dockerfilePath": "Dockerfile"
  },
  "deploy": {
    "restartPolicyType": "ON_FAILURE",
    "restartPolicyMaxRetries": 10
  }
}
EOF

    # Create .gitignore
    cat > .gitignore << 'EOF'
## Ignore Visual Studio temporary files, build results, and
## files generated by popular Visual Studio add-ons.

# User-specific files
*.suo
*.user
*.userosscache
*.sln.docstates

# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
bld/
[Bb]in/
[Oo]bj/

# .NET Core
project.lock.json
project.fragment.lock.json
artifacts/
**/Properties/launchSettings.json

# Logs
logs/
*.log

# Database
*.db
*.sqlite

# Environment variables
.env

# VS Code
.vscode/

# Rider
.idea/
EOF

    print_status "Configuration files created successfully"
}

# Create logs directory
create_logs_directory() {
    print_info "Creating logs directory..."
    mkdir -p InventorySystem.API/logs
    print_status "Logs directory created"
}

# Run migrations
run_migrations() {
    print_info "Applying database migrations..."
    
    cd InventorySystem.API
    
    # Add initial migration
    print_info "Creating initial migration..."
    dotnet ef migrations add InitialCreate --project ../InventorySystem.Infrastructure --startup-project .
    
    if [ $? -eq 0 ]; then
        print_status "Migration created successfully"
        
        # Apply migration
        print_info "Applying migration to database..."
        dotnet ef database update --project ../InventorySystem.Infrastructure --startup-project .
        
        if [ $? -eq 0 ]; then
            print_status "Database migration applied successfully"
        else
            print_error "Failed to apply database migration"
            return 1
        fi
    else
        print_error "Failed to create migration"
        return 1
    fi
    
    cd ..
}

# Test the application
test_application() {
    print_info "Testing the application..."
    
    cd InventorySystem.API
    
    # Build the application
    print_info "Building the application..."
    dotnet build
    
    if [ $? -eq 0 ]; then
        print_status "Application built successfully"
        
        # Start the application in background for testing
        print_info "Starting application for testing..."
        dotnet run &
        APP_PID=$!
        
        # Wait a few seconds for the app to start
        sleep 10
        
        # Test health endpoint
        if curl -s http://localhost:5000/health > /dev/null; then
            print_status "Health check endpoint working"
        else
            print_warning "Health check endpoint not responding"
        fi
        
        # Test Swagger endpoint
        if curl -s http://localhost:5000/swagger > /dev/null; then
            print_status "Swagger UI accessible"
        else
            print_warning "Swagger UI not accessible"
        fi
        
        # Stop the test application
        kill $APP_PID 2>/dev/null
        
    else
        print_error "Failed to build application"
        return 1
    fi
    
    cd ..
}

# Create seed data script
create_seed_script() {
    print_info "Creating seed data script..."
    
    cat > seed-data.sh << 'EOF'
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
EOF

    chmod +x seed-data.sh
    print_status "Seed data script created: seed-data.sh"
}

# Main execution
main() {
    echo "🎯 Starting Inventory Management System Setup"
    echo "=============================================="
    
    check_dotnet
    create_project_structure
    configure_references
    install_packages
    setup_postgresql
    create_config_files
    create_logs_directory
    create_seed_script
    
    echo ""
    echo "📋 Setup Summary:"
    echo "=================="
    print_status "✅ .NET 9 environment ready"
    print_status "✅ Project structure created"
    print_status "✅ NuGet packages installed"
    print_status "✅ PostgreSQL configured"
    print_status "✅ Configuration files created"
    print_status "✅ Logging configured"
    print_status "✅ Docker files created"
    print_status "✅ Railway deployment ready"
    
    echo ""
    echo "🚀 Next Steps:"
    echo "=============="
    echo "1. Copy the entity classes from the artifacts"
    echo "2. Copy the repository implementations"
    echo "3. Copy the service implementations"
    echo "4. Copy the controller implementations"
    echo "5. Run migrations:"
    echo "   cd InventorySystem.API"
    echo "   dotnet ef migrations add InitialCreate --project ../InventorySystem.Infrastructure"
    echo "   dotnet ef database update --project ../InventorySystem.Infrastructure"
    echo "6. Start the application:"
    echo "   dotnet run --project InventorySystem.API"
    echo "7. Load sample data:"
    echo "   ./seed-data.sh"
    echo ""
    echo "🌐 For Railway deployment:"
    echo "========================="
    echo "1. Push to GitHub: git init && git add . && git commit -m 'Initial commit'"
    echo "2. Go to railway.app and connect your GitHub repo"
    echo "3. Add PostgreSQL service in Railway dashboard"
    echo "4. Deploy automatically!"
    echo ""
    echo "🎉 Setup complete! Your Inventory Management System is ready!"
}

# Run main function
main "$@"

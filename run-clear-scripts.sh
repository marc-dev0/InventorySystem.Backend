#!/bin/bash

# Script para ejecutar limpieza de base de datos
# Asegúrate de tener configurada la conexión a PostgreSQL

DB_HOST="localhost"
DB_PORT="5432"
DB_NAME="inventory_db"
DB_USER="marc"

echo "=== SCRIPTS DE LIMPIEZA DE BASE DE DATOS ==="
echo "Asegúrate de hacer backup antes de continuar"
echo ""

echo "Opciones disponibles:"
echo "1. Limpiar solo stock e inventario (conservativo)"
echo "2. Limpiar todos los datos de productos (completo)"
echo "3. Cancelar"
echo ""

read -p "Selecciona una opción (1-3): " option

case $option in
    1)
        echo "Ejecutando limpieza de stock solamente..."
        psql -h $DB_HOST -p $DB_PORT -d $DB_NAME -U $DB_USER -f clear-only-stock.sql
        ;;
    2)
        echo "⚠️  ADVERTENCIA: Esto eliminará TODOS los datos de productos, ventas, compras, etc."
        read -p "¿Estás seguro? Escribe 'SI' para continuar: " confirm
        if [ "$confirm" = "SI" ]; then
            echo "Ejecutando limpieza completa..."
            psql -h $DB_HOST -p $DB_PORT -d $DB_NAME -U $DB_USER -f clear-all-data.sql
        else
            echo "Operación cancelada"
        fi
        ;;
    3)
        echo "Operación cancelada"
        exit 0
        ;;
    *)
        echo "Opción inválida"
        exit 1
        ;;
esac

echo "Limpieza completada"
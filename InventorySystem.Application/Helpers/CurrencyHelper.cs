using System.Globalization;

namespace InventorySystem.Application.Helpers
{
    /// <summary>
    /// Helper class for currency formatting throughout the application
    /// </summary>
    public static class CurrencyHelper
    {
        private static readonly CultureInfo PeruvianCulture = new CultureInfo("es-PE");

        /// <summary>
        /// Formats a decimal value as Peruvian Soles currency
        /// </summary>
        /// <param name="amount">The amount to format</param>
        /// <returns>Formatted currency string (e.g., "S/1,234.56")</returns>
        public static string FormatCurrency(decimal amount)
        {
            return amount.ToString("C", PeruvianCulture);
        }

        /// <summary>
        /// Formats a decimal value as Peruvian Soles currency without decimals
        /// </summary>
        /// <param name="amount">The amount to format</param>
        /// <returns>Formatted currency string without decimals (e.g., "S/1,234")</returns>
        public static string FormatCurrencyWithoutDecimals(decimal amount)
        {
            return amount.ToString("C0", PeruvianCulture);
        }

        /// <summary>
        /// Formats a decimal value as a number with thousand separators (no currency symbol)
        /// </summary>
        /// <param name="amount">The amount to format</param>
        /// <returns>Formatted number string (e.g., "1,234.56")</returns>
        public static string FormatNumber(decimal amount)
        {
            return amount.ToString("N", PeruvianCulture);
        }

        /// <summary>
        /// Formats a decimal value as a number without decimals
        /// </summary>
        /// <param name="amount">The amount to format</param>
        /// <returns>Formatted number string without decimals (e.g., "1,234")</returns>
        public static string FormatNumberWithoutDecimals(decimal amount)
        {
            return amount.ToString("N0", PeruvianCulture);
        }
    }
}
namespace Staybnb.Models
{
    public static class StringExtensions
    {
        /// <summary>
        /// Safely truncates a string to ensure it fits within maximum column string lengths,
        /// avoiding DbUpdateException / SqlException truncation errors.
        /// </summary>
        public static string Truncate(this string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}

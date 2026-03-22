using System.Globalization;

namespace PourDecisions.Shared.Extensions;

/// <summary>
/// Provides extension methods for string manipulation.
/// </summary>
public static class StringExtensions
{
    extension(string str)
    {
        /// <summary>
        /// Converts the specified string to the title case - first letter of each word capitalized.
        /// </summary>
        /// <returns>The specified string converted to the title case.</returns>
        public string ToTitleCase()
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.Trim().ToLower());
        }
    }
}

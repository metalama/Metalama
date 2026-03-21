using System.Resources;

namespace System
{
    internal static partial class SR
    {
        private static readonly bool _usingResourceKeys;

        public static string? GetResourceString(string resourceKey)
        {
            string? resourceString = ResourceManager.GetString(resourceKey);

            return resourceString;
        }
    }
}

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Shared.Security
{
    public static class ApiKeyValidator
    {
        public const string HeaderName = "Service-Api-Key";

        public static bool IsValid(HttpRequest request, string expectedKey)
        {
            if (string.IsNullOrEmpty(expectedKey))
            {
                return false;
            }

            if (!request.Headers.TryGetValue(HeaderName, out var providedValues))
            {
                return false;
            }

            var provided = providedValues.ToString();
            if (string.IsNullOrEmpty(provided))
            {
                return false;
            }

            var providedBytes = Encoding.UTF8.GetBytes(provided);
            var expectedBytes = Encoding.UTF8.GetBytes(expectedKey);

            return providedBytes.Length == expectedBytes.Length
                && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
        }
    }
}

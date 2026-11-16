using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Serialization
{
    public static class JsonDefaults
    {
        public static JsonSerializerOptions Api { get; } = Create();

        private static JsonSerializerOptions Create()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }
    }
}

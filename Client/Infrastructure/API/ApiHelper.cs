using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Client.Infrastructure.API
{
    public static class ApiHelper
    {
        private static readonly JsonSerializerOptions DefaultJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Создаёт StringContent с application/json БЕЗ charset
        /// </summary>
        public static StringContent CreateJsonContent<T>(T data, JsonSerializerOptions? options = null)
        {
            var json = JsonSerializer.Serialize(data, options ?? DefaultJsonOptions);
            var content = new StringContent(json, Encoding.UTF8);
            // Явно устанавливаем Content-Type без charset
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            return content;
        }
    }
}

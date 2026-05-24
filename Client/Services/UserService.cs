using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Client.Core.Interfaces;
using Client.Core.Interfaces.DTO.API;
using Client.Infrastructure.API;
using Microsoft.Extensions.Options;

namespace Client.Services
{
    public class UserService(IHttpClientFactory httpClientFactory, IOptions<ApiSettings> options) : IUserService
    {
        private readonly HttpClient _httpClient = httpClientFactory?.CreateClient("GarminApi") ?? throw new ArgumentNullException(nameof(httpClientFactory));
        private readonly ApiSettings _settings = options.Value ?? throw new ArgumentNullException(nameof(options));

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public async Task<UserProfileDto> GetCurrentUserAsync(CancellationToken ct = default)
        {
            var response = await _httpClient.GetAsync($"{_settings.BaseUrl}/users/me", ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Токен истек или недействителен");

            response.EnsureSuccessStatusCode();

            var user = await response.Content.ReadFromJsonAsync<UserProfileDto>(JsonOptions, ct);

            return user ?? throw new InvalidOperationException("Пустой ответ отсервера");
        }

        public async Task UpdateCurrentUserAsync(UpdateUserRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!request.HasChanges)
                throw new ArgumentException("Нет полей ля обновления", nameof(request));

            var response = await _httpClient.PutAsJsonAsync
                ($"{_settings.BaseUrl}/users/me",
                request,
                JsonOptions,
                ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Токен истек или недействителен");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new InvalidOperationException("Пользователь не найден");

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new ArgumentException($"Ошибка валидации: {error}");
            }

            response.EnsureSuccessStatusCode();
        }
    }


}

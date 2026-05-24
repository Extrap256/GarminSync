using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Client.Core.Interfaces;
using Client.Core.Interfaces.DTO.API;
using Client.Infrastructure.API;
using Microsoft.Extensions.Options;

namespace Client.Services
{
    public class ReportService(IHttpClientFactory httpClientFactory, IOptions<ApiSettings> options) : IReportService
    {
        private readonly HttpClient _httpClient = httpClientFactory?.CreateClient("GarminApi") ?? throw new ArgumentNullException(nameof(httpClientFactory));
        private readonly ApiSettings _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public async Task<PersonalReportResponse> GetPersonalReportAsync(
            PersonalReportRequest request,
            CancellationToken ct = default)
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{_settings.BaseUrl}/reports/personal",
                request,
                JsonOptions,
                ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Токен истек");

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PersonalReportResponse>(
                JsonOptions, ct);

            return result ?? throw new InvalidOperationException("Пустой ответ сервера");
        }
    }
}

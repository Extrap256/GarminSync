namespace Client.Infrastructure.Security.Interfaces
{
    public interface ITokenStorage
    {
        /// <summary>
        /// Сохранение токена
        /// </summary>
        Task SaveTokenAsync(string token, DateTime expiresAt, CancellationToken ct = default);
        /// <summary>
        /// Очистка токена
        /// </summary>
        Task ClearTokenAsync(CancellationToken ct = default);
        /// <summary>
        /// Получение текущего токена
        /// </summary>
        string? GetToken();
        /// <summary>
        /// Получение время истечения токена
        /// </summary>
        DateTime? GetTokenExpiresAt();
    }
}

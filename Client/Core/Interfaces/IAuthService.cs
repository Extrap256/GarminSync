using Client.Core.Interfaces.DTO.API;

namespace Client.Core.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        Task<AuthResponse> RegisterAsync(RegisterRequest RegisterRequest, CancellationToken ct = default);        
        /// <summary>
        /// Вход в аккаунт
        /// </summary>
        Task<AuthResponse> LoginAsync(LoginRequest loginRequest, CancellationToken ct = default);
        /// <summary>
        /// Выход из системы
        /// </summary>
        Task LogoutAsync(CancellationToken ct = default);
        /// <summary>
        /// Локальная проверка на авторизацию
        /// </summary>
        bool IsAuthenticated();
        /// <summary>
        /// Получение текущего токена
        /// </summary>
        string? GetToken();
    }
}

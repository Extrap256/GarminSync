using Client.Core.Interfaces.DTO;
using Client.Core.Interfaces.DTO.API;
using Client.Core.Interfaces.Enums;

namespace Client.Core.Interfaces
{
    public interface IAppUI
    {
        /// <summary>
        /// Работа с профилем (вход, регистрация, работа локально)
        /// </summary>
        Task<AuthAction> ShowAuthMenuAsync(CancellationToken ct = default);
        
        /// <summary>
        /// Вывод профиля с возможностью изменения
        /// </summary>
        Task ProfileMenuAsync(IUserService userService, CancellationToken ct = default);
        
        /// <summary>
        /// Метод для входа пользователя в систему
        /// </summary>
        /// <returns>Запрос с данными для входа</returns>
        Task<LoginRequest> PromptLoginAsync(CancellationToken ct = default);

        /// <summary>
        /// Метод для регистрации нового пользователя
        /// </summary>
        /// <returns>Запрос с даннными регистрации</returns>
        Task<RegisterRequest> PromptRegisterAsync(CancellationToken ct = default);

        /// <summary>
        /// Выбор устройства для работы
        /// </summary>
        /// <param name="devices">Список всех доступных устройств</param>
        Task<DeviceInfo?> SelectDeviceAsync(IReadOnlyList<DeviceInfo> devices, CancellationToken ct = default);
        void ShowMessage(string message);
        void ShowError(string message);
        void ShowProgress(string fileName, int current, int total);
        void ShowHeader();
        bool Confirm(string question);
        void WaitForContinue();
    }
}

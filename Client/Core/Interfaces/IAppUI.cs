using Client.Core.Interfaces.DTO;

namespace Client.Core.Interfaces
{
    public interface IAppUI
    {
        /// <summary>
        /// Выбор устройства для работы
        /// </summary>
        /// <param name="devices">Список всех доступных устройств</param>
        Task<DeviceInfo?> SelectDeviceAsync(IReadOnlyList<DeviceInfo> devices, CancellationToken ct = default);
        void ShowMessage(string message);
        void ShowError(string message);
        void ShowProgress(string fileName, int current, int total);
    }
}

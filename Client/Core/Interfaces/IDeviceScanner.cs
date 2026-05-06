using Client.Core.Interfaces.DTO;

namespace Client.Core.Interfaces
{
    public interface IDeviceScanner
    {
        /// <summary>
        /// Сканируем систему на наличие подключенного устройства Garmin
        /// </summary>
        Task<IReadOnlyList<DeviceInfo>> ScanAsync(CancellationToken ct = default);
    }
}

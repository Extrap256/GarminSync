using Client.Core.Interfaces.DTO;

namespace Client.Core.Interfaces
{
    public interface IFitParser
    {
        /// <summary>
        /// Парсит .fit файл и возвращает DTO тренировки
        /// </summary>
        Task<FitDataCollector> ParseActivityAsync(string filePath, CancellationToken ct = default);
        /// <summary>
        /// Парсит .fit файл и возвращает DTO устройства
        /// </summary>
        Task<DeviceData> ParseDeviceAsync(string filePath, CancellationToken ct = default);
    }
}

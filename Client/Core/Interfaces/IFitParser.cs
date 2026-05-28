using Client.Core.Interfaces.DTO;

namespace Client.Core.Interfaces
{
    public interface IFitParser
    {
        /// <summary>
        /// Парсит .fit файл и возвращает DTO тренировки
        /// </summary>
        //Task<FitDataCollector> ParseActivityAsync(string filePath, CancellationToken ct = default);
        
        /// <summary>
        /// Парсит .fit файл и возвращает DTO устройства
        /// </summary>
        Task<DeviceData> ParseDeviceAsync(string filePath, DeviceInfo? deviceInfo = null, CancellationToken ct = default);
        /// <summary>
        /// Парсит .fit файл и возвращает DTO тренировки
        /// </summary>
        /// <param name="filePath">Путь к файлу (локальный или MTP)</param>
        /// <param name="deviceInfo">Информация об устройстве (для определения типа чтения)</param>
        /// <param name="ct">Токен отмены</param>
        Task<FitDataCollector> ParseActivityAsync(
            string filePath,
            DeviceInfo? deviceInfo = null,
            CancellationToken ct = default);
    }
}

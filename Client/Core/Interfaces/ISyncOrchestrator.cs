using Client.Core.Interfaces.DTO;

namespace Client.Core.Interfaces
{
    public interface ISyncOrchestrator
    {
        /// <summary>
        /// Выполнение парсинга и сохранение в БД всех файлов тренировок
        /// </summary>
        Task<SyncResult> RunAsync(DeviceInfo deviceInfo, CancellationToken ct = default);
    }
}

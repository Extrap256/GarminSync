using Client.Core.Interfaces.DTO;

namespace Client.Core.Interfaces
{
    public interface ISyncOrchestrator
    {
        /// <summary>
        /// Выполнение парсинга и сохранение в БД всех файлов тренировок
        /// </summary>
        Task<SyncResult> RunAsync(string activitiesFolderPath, CancellationToken ct = default);
    }
}

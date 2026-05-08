using Client.Core.Interfaces.DTO;
namespace Client.Core.Interfaces
{
    public interface IFitParser
    {
        /// <summary>
        /// Парсит .fit файл и возвращает DTO тренировки
        /// </summary>
        Task<FitDataCollector> ParseAsync(string filePath, CancellationToken ct = default);
    }
}

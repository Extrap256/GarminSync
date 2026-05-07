using Client.Core.Interfaces.DTO;
using Client.Storage.Models;
namespace Client.Core.Interfaces
{
    public interface IFitParser
    {
        /// <summary>
        /// Парсит .fit файл и возвращает класс сущности тренировки
        /// </summary>
        Task<FitDataCollector> ParseAsync(string filePath, CancellationToken ct = default);
    }
}

using Client.Storage.Models;
namespace Client.Core.Interfaces
{
    public interface IFitParser
    {
        /// <summary>
        /// Парсит .fit файл и возвращает класс сущности тренировки
        /// </summary>
        Task<Workout> ParseAsync(string filePath, CancellationToken ct = default);
    }
}

using EntityClassLibraryClient.Models;

namespace Client.Core.Interfaces
{
    public interface ILocalRepository
    {
        /// <summary>
        /// Проверка на существование тренировки
        /// </summary>
        /// <param name="identifier">Уникальное поле тренировки</param>
        Task<bool> ExistByIdentifierAsync(string identifier, CancellationToken ct = default);

        /// <summary>
        /// Добавление сущности тренировки в БД контекст
        /// </summary>
        /// <param name="workout">Сущность тренировки</param>
        /// <param name="SerialNumberDevice">Серийный номер устройства, к которому будет привязана тренировка</param>
        Task AddAsync(Workout workout, uint SerialNumberDevice, CancellationToken ct = default);

        /// <summary>
        /// Сохранение изменений в БД
        /// </summary>
        Task SaveChangeAsync(CancellationToken ct = default);
        /// <summary>
        /// Возвращает устройство или null при отсутствии
        /// </summary>
        /// <param name="serialNumber">Серийный номер устройства</param>
        /// <returns></returns>
        Task<Device> GetOrCreateDeviceAsync(uint serialNumber);
    }
}
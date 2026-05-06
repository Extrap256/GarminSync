namespace Client.Core.Interfaces.DTO
{
    /// <summary>
    /// DTO для результата работы оркестратора
    /// </summary>
    /// <param name="Processed">Успешно обработаны</param>
    /// <param name="Skipped">Пропущены по известным причинам</param>
    /// <param name="Errors">Список ошибок для файлов, которые не удалось обработать</param>
    public record SyncResult(int Processed, int Skipped, IReadOnlyList<string> Errors);
}

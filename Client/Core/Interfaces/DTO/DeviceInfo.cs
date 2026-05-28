using MediaDevices;

namespace Client.Core.Interfaces.DTO
{
    /// <summary>
    /// DTO подключенного устройства
    /// </summary>
    /// <param name="Label">Отображаемое имя устройства</param>
    /// <param name="DrivePath">Корень подключенного устройства</param>
    /// <param name="ActivitiesPath">Путь до папки с тренировками</param>
    public record DeviceInfo(string Label, string DrivePath, string ActivitiesPath, string DeviceInfoPath, bool IsMtpDevice = false, MediaDevice? MtpDevice = null);
}

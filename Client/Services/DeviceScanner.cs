using Client.Core.Interfaces;
using Client.Core.Interfaces.DTO;
using MediaDevices;

namespace Client.Services
{
    public class DeviceScanner(string activitiesPath = "GARMIN\\ACTIVITY", string deviceInfoPath = "GARMIN\\Device.fit") : IDeviceScanner
    {
        private readonly string _activityPath = activitiesPath;
        private readonly string _deviceInfoPath = deviceInfoPath;

        public async Task<IReadOnlyList<DeviceInfo>> ScanAsync(CancellationToken ct = default)
        {
            var devices = new List<DeviceInfo>();

            var usbDevices = ScanUsbMassStorage(ct);
            devices.AddRange(usbDevices);

            var mtpDevices = await ScanMtpDevicesAsync(ct);
            devices.AddRange(mtpDevices);

            return devices;
        }

        private List<DeviceInfo> ScanUsbMassStorage(CancellationToken ct)
        {
            var devices = new List<DeviceInfo>();
            var drivers = DriveInfo.GetDrives();

            foreach (var drive in drivers)
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    if (!drive.IsReady)
                        continue;

                    var activitiesPath = Path.Combine(drive.Name, _activityPath);
                    var deviceInfoPath = Path.Combine(drive.Name, _deviceInfoPath);


                    if (!Directory.Exists(activitiesPath))
                        continue;

                    devices.Add(new DeviceInfo(
                        Label: string.IsNullOrWhiteSpace(drive.VolumeLabel)
                            ? "USB Device"
                            : drive.VolumeLabel,
                        DrivePath: drive.Name,
                        ActivitiesPath: activitiesPath,
                        IsMtpDevice: false,
                        MtpDevice: null,
                        DeviceInfoPath: deviceInfoPath
                    ));
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }

            return devices;
        }

        private async Task<List<DeviceInfo>> ScanMtpDevicesAsync(CancellationToken ct)
        {
            var devices = new List<DeviceInfo>();

            try
            {
                // MediaDevices не поддерживает async, оборачиваем в Task.Run
                var mtpDevices = await Task.Run(() => MediaDevice.GetDevices(), ct);

                foreach (var mtpDevice in mtpDevices)
                {
                    if (ct.IsCancellationRequested)
                        break;

                    try
                    {
                        // Подключаемся к устройству
                        mtpDevice.Connect();

                        var root = mtpDevice.GetRootDirectory();
                        var storageFolder = root.EnumerateDirectories().FirstOrDefault();

                        if (storageFolder == null)
                            throw new DriveNotFoundException("В корне MTP-устройства отсутсвуют папки");

                        // Проверяем, есть ли папка GARMIN
                        string garminPath = $"{storageFolder.FullName}/Garmin";
                        string activityPath = $"{storageFolder.FullName}/{_activityPath.Replace(@"\", @"/")}";
                        string deviceFitPath = $"{storageFolder.FullName}/{_deviceInfoPath.Replace(@"\", @"/")}";

                        if (mtpDevice.DirectoryExists(activityPath))
                        {
                            devices.Add(new DeviceInfo(
                            Label: $"[MTP] {mtpDevice.FriendlyName}",
                            DrivePath: $"MTP://{mtpDevice.FriendlyName}",
                            ActivitiesPath: activityPath,
                            IsMtpDevice: true,
                            MtpDevice: mtpDevice,
                            DeviceInfoPath: deviceFitPath));
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                    finally
                    {
                        mtpDevice.Disconnect();
                        mtpDevice.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return devices;
        }
    }
}

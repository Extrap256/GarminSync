using Client.Core.Interfaces;
using Client.Core.Interfaces.DTO;

namespace Client.Services
{
    public class DeviceScanner(string activitiesPath = "GARMIN\\ACTIVITY") : IDeviceScanner
    {
        private string _activityPath = activitiesPath;

        public Task<IReadOnlyList<DeviceInfo>> ScanAsync(CancellationToken token)
        {
            DriveInfo[] drives = DriveInfo.GetDrives();

            List<DeviceInfo> devices = [];

            foreach (DriveInfo drive in drives)
            {
                if (token.IsCancellationRequested)
                {
                    return Task.FromResult<IReadOnlyList<DeviceInfo>>(devices);
                }

                if (drive.IsReady)
                {
                    var fullPath = Path.Combine(drive.Name, _activityPath);

                    if (Directory.Exists(fullPath))
                    {
                        devices.Add(new(
                            Label: string.IsNullOrEmpty(drive.VolumeLabel) ? "Без названия" : drive.VolumeLabel,
                            DrivePath: drive.Name,
                            ActivitiesPath: fullPath));
                    }
                }
            }
            

            return Task.FromResult<IReadOnlyList<DeviceInfo>>(devices);
        }
    }
}

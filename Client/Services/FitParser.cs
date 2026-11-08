using Client.Core.Interfaces;
using Client.Core.Interfaces.DTO;
using Dynastream.Fit;
using MediaDevices;

namespace Client.Services
{
    public class FitParser : IFitParser
    {
        public Task<FitDataCollector> ParseActivityAsync(string filePath, DeviceInfo? deviceInfo = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var collector = new FitDataCollector();

            if (deviceInfo?.IsMtpDevice == true && deviceInfo.MtpDevice != null)
            {
                ParseFromMtp(deviceInfo.MtpDevice, filePath, collector);
            }
            else
            {
                ParseFromFile(filePath, collector);
            }

            return Task.FromResult(collector);
        }

        private static void ParseFromFile(string filePath, FitDataCollector collector)
        {
            if (!System.IO.File.Exists(filePath))
                throw new FileNotFoundException($"Файл не найден: {filePath}");

            var decoder = new Decode();
            var broadcaster = new MesgBroadcaster();

            decoder.MesgEvent += broadcaster.OnMesg;
            decoder.MesgDefinitionEvent += broadcaster.OnMesgDefinition;

            SubscribeToEvents(broadcaster, collector);

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            if (!decoder.Read(stream))
                throw new InvalidDataException($"Невалидный FIT-файл: {filePath}");
        }
        private static void ParseFromMtp(MediaDevice device, string mtpPath, FitDataCollector collector)
        {
            if (!device.IsConnected)
                throw new NotConnectedException("Устройство MTR не подключено");

            var decoder = new Decode();
            var broadcaster = new MesgBroadcaster();

            decoder.MesgEvent += broadcaster.OnMesg;
            decoder.MesgDefinitionEvent += broadcaster.OnMesgDefinition;

            SubscribeToEvents(broadcaster, collector);

            // Конвертируем путь из "MTP://Garmin/GARMIN/Activities/file.fit" в "/GARMIN/Activities/file.fit"
            //var relativePath = ConvertMtpPath(mtpPath);
            var relativePath = mtpPath;
            try
            {
                // Читаем файл в MemoryStream
                using var memoryStream = new MemoryStream();
                using (var mtpStream = device.OpenReadFromPersistentUniqueId(device.GetFileInfo(relativePath).PersistentUniqueId))
                {
                    mtpStream.CopyTo(memoryStream);
                }

                // Сбрасываем позицию для чтения
                memoryStream.Position = 0;

                // Парсим из памяти
                if (!decoder.Read(memoryStream))
                    throw new InvalidDataException($"Невалидный FIT-файл: {mtpPath}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка чтения MTP-файла: {ex.Message}", ex);
            }
        }

        private static void SubscribeToEvents(MesgBroadcaster broadcaster, FitDataCollector collector)
        {
            broadcaster.ActivityMesgEvent += (s, e) => OnActivityMesg(e, collector);
            //broadcaster.DeviceInfoMesgEvent += (s, e) => OnDeviceInfoMesg(e, collector);
            //broadcaster.EventMesgEvent += (s, e) => OnEventMesg(e, collector);
            broadcaster.FileIdMesgEvent += (s, e) => OnFileIdMesg(e, collector);
            broadcaster.SessionMesgEvent += (s, e) => OnSessionMesg(e, collector);
            broadcaster.RecordMesgEvent += (s, e) => OnRecordMesg(e, collector);
            broadcaster.LapMesgEvent += (s, e) => OnLapMesg(e, collector);
        }

        public Task<DeviceData> ParseDeviceAsync(string filePath, DeviceInfo? deviceInfo = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var deviceData = new DeviceData();

            if (deviceInfo?.IsMtpDevice == true && deviceInfo.MtpDevice != null)
            {
                ParseDeviceFromMtp(deviceInfo.MtpDevice, filePath, deviceData);
            }
            else
            {
                ParseDeviceFromFile(filePath, deviceData);
            }

            return Task.FromResult(deviceData);
        }
        private static void ParseDeviceFromMtp(MediaDevice device, string filePath, DeviceData deviceData)
        {
            if (!device.IsConnected)
                throw new NotConnectedException("Устройство MTR не подключено");

            var decoder = new Decode();
            var broadcaster = new MesgBroadcaster();

            decoder.MesgEvent += broadcaster.OnMesg;
            decoder.MesgDefinitionEvent += broadcaster.OnMesgDefinition;

            broadcaster.DeviceInfoMesgEvent += (s, e) => OnFileIdMesg(e, deviceData);

            // Конвертируем путь из "MTP://Garmin/GARMIN/Activities/file.fit" в "/GARMIN/Activities/file.fit"
            //var relativePath = ConvertMtpPath(filePath);
            var relativePath = filePath;
            try
            {
                // Читаем файл в MemoryStream
                using var memoryStream = new MemoryStream();
                using (var mtpStream = device.OpenReadFromPersistentUniqueId(device.GetFileInfo(relativePath).PersistentUniqueId))
                {
                    if (!decoder.Read(memoryStream))
                        throw new InvalidDataException($"Невалидный FIT-файл: {filePath}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка чтения MTP-файла: {ex.Message}", ex);
            }
        }
        private static void ParseDeviceFromFile(string filePath, DeviceData deviceData)
        {
            var fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists)
                throw new FileNotFoundException(filePath);

            Decode decoder = new();
            MesgBroadcaster broadcaster = new();

            decoder.MesgEvent += broadcaster.OnMesg;
            decoder.MesgDefinitionEvent += broadcaster.OnMesgDefinition;

            broadcaster.DeviceInfoMesgEvent += (s, e) => OnFileIdMesg(e, deviceData);

            try
            {
                using var stream = new FileStream(filePath, FileMode.Open);

                decoder.Read(stream);

                return;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Ошибка парсинга FIT-файла: {filePath}", ex);
            }
        }
        static private void OnLapMesg(MesgEventArgs e, FitDataCollector fitDataCollector)
        {
            var msg = e.mesg as LapMesg;
            
            LapDataCollection lap = new()
            {
                TimeEndUtc = msg?.GetTimestamp().GetDateTime(),
                AvgCadence = msg?.GetAvgCadence() ?? msg?.GetAvgRunningCadence(),
                AvgHeartRate = msg?.GetAvgHeartRate(),
                AvgPower = msg?.GetAvgPower(),
                AvgSpeedMetersPerHour = (uint?)(msg?.GetAvgSpeed() * 3600),
                DistanceMeters = (uint?)msg?.GetTotalDistance(),
                ElevationGain = msg?.GetTotalAscent(),
            };

            fitDataCollector.LapDataCollections.Add(lap);
        }
        static private void OnFileIdMesg(MesgEventArgs e, FitDataCollector fitDataCollector)
        {
            var msg = e.mesg as FileIdMesg;

            fitDataCollector.DeviceData.SerialNumber = msg?.GetSerialNumber() ?? throw new Exception("Отсутствует SerialNumber");
            fitDataCollector.DeviceData.Product = msg?.GetProduct();
        }
        static private void OnFileIdMesg(MesgEventArgs e, DeviceData deviceData)
        {
            var msg = e.mesg as FileIdMesg;

            deviceData.SerialNumber = msg?.GetSerialNumber() ?? throw new Exception("Отсутствует SerialNumber");
            deviceData.Product = msg?.GetProduct();
        }
        static private void OnSessionMesg(MesgEventArgs e, FitDataCollector fitDataCollector)
        {
            var msg = e.mesg as SessionMesg;

            fitDataCollector.SportType = msg?.GetSport() switch 
            { 
                Sport.Running => SportTypes.Running,
                Sport.Cycling=> SportTypes.Cycling,
                Sport.CrossCountrySkiing => SportTypes.Skiing,
                _ => SportTypes.Other
            };

            fitDataCollector.DistanceMeters = (uint?)msg?.GetTotalDistance();
            fitDataCollector.TimeStartUtc = msg?.GetStartTime().GetDateTime();
            fitDataCollector.TimeEndUtc = msg?.GetStartTime().GetDateTime().AddSeconds((double?)msg?.GetTotalElapsedTime() ?? 0);
            fitDataCollector.AvgSpeedMetersPerHour = (uint?)(msg?.GetAvgSpeed() * 3600);
            fitDataCollector.AvgHeartRate = msg?.GetAvgHeartRate();
            fitDataCollector.ElevationGain = msg?.GetTotalAscent();

            fitDataCollector.AvgPower = msg?.GetAvgPower();
            fitDataCollector.AvgCadence = msg?.GetAvgCadence();
            fitDataCollector.StyleOfSkiing = msg?.GetSubSport() == SubSport.SkateSkiing ? FitDataCollector.SkiStyle.Skate : FitDataCollector.SkiStyle.Classic;
        }
        static private void OnRecordMesg(MesgEventArgs e, FitDataCollector fitDataCollector)
        {
            var msg = e.mesg as RecordMesg;

            RecordDataCollection record = new()
            {
                TimeUtc = msg?.GetTimestamp().GetDateTime(),
                DistanceMeters = (uint?)msg?.GetDistance(),
                Altitude = (ushort?)msg?.GetAltitude(),
                Cadence = msg?.GetCadence(),
                HeartRate = msg?.GetHeartRate(),
                Latitude = msg?.GetPositionLat(),
                Longitude = msg?.GetPositionLong(),
                Power = msg?.GetPower(),
                SpeedMetersPerHour = (uint?)(msg?.GetSpeed() * 3600)
            };

            fitDataCollector.RecordDataCollections.Add(record);
        }
        static private void OnEventMesg(MesgEventArgs e, FitDataCollector fitDataCollector)
        {
            // Нечего
        }
        static private void OnDeviceInfoMesg(MesgEventArgs e, FitDataCollector fitDataCollector)
        {
            // нечего
        }
        
        static private void OnActivityMesg(MesgEventArgs e, FitDataCollector fitDataCollector)
        {
            var msg = new ActivityMesg(e.mesg);

            uint? localTimeRaw = msg?.GetLocalTimestamp();
            uint? utcTimeRaw = msg?.GetTimestamp()?.GetTimeStamp();

            if (localTimeRaw != null && utcTimeRaw != null)
                fitDataCollector.OffsetInSeconds = (int)((long)localTimeRaw - (long)utcTimeRaw);
            else
                fitDataCollector.OffsetInSeconds = 0;
        }
    }
}

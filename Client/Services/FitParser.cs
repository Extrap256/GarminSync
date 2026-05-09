using System.Text;
using Client.Core.Interfaces;
using Client.Core.Interfaces.DTO;
using Dynastream.Fit;

namespace Client.Services
{
    public class FitParser : IFitParser
    {
        public Task<FitDataCollector> ParseActivityAsync(string filePath, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            var fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists)
                throw new FileNotFoundException(filePath);

            Decode decoder = new();
            MesgBroadcaster broadcaster = new();
            FitDataCollector fitDataCollector = new();

            decoder.MesgEvent += broadcaster.OnMesg;
            decoder.MesgDefinitionEvent += broadcaster.OnMesgDefinition;

            broadcaster.ActivityMesgEvent += (s, e) => OnActivityMesg(e, fitDataCollector);
            //broadcaster.DeviceInfoMesgEvent += (s, e) => OnDeviceInfoMesg(e, fitDataCollector);
            //broadcaster.EventMesgEvent += (s, e) => OnEventMesg(e, fitDataCollector);
            broadcaster.FileIdMesgEvent += (s, e) => OnFileIdMesg(e, fitDataCollector);
            broadcaster.SessionMesgEvent += (s, e) => OnSessionMesg(e, fitDataCollector);
            broadcaster.RecordMesgEvent += (s, e) => OnRecordMesg(e, fitDataCollector);
            broadcaster.LapMesgEvent += (s, e) => OnLapMesg(e, fitDataCollector);

            try
            {
                using var stream = new FileStream(filePath, FileMode.Open);

                decoder.Read(stream);

                return Task.FromResult(fitDataCollector);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Ошибка парсинга FIT-файла: {filePath}", ex);
            }
        }
        public Task<DeviceData> ParseDeviceAsync(string filePath, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists)
                throw new FileNotFoundException(filePath);

            Decode decoder = new();
            MesgBroadcaster broadcaster = new();
            DeviceData deviceData = new();

            decoder.MesgEvent += broadcaster.OnMesg;
            decoder.MesgDefinitionEvent += broadcaster.OnMesgDefinition;

            broadcaster.DeviceInfoMesgEvent += (s, e) => OnFileIdMesg(e, deviceData);

            try
            {
                using var stream = new FileStream(filePath, FileMode.Open);

                decoder.Read(stream);

                return Task.FromResult(deviceData);
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
        static public void OnFileIdMesg(MesgEventArgs e, FitDataCollector fitDataCollector)
        {
            var msg = e.mesg as FileIdMesg;

            fitDataCollector.DeviceData.SerialNumber = msg?.GetSerialNumber();
            fitDataCollector.DeviceData.Product = msg?.GetProduct();
        }
        static public void OnFileIdMesg(MesgEventArgs e, DeviceData deviceData)
        {
            var msg = e.mesg as FileIdMesg;

            deviceData.SerialNumber = msg?.GetSerialNumber();
            deviceData.Product = msg?.GetProduct();
        }
        static public void OnSessionMesg(MesgEventArgs e, FitDataCollector fitDataCollector)
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
        static public void OnRecordMesg(MesgEventArgs e, FitDataCollector fitDataCollector)
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
        static public void OnEventMesg(MesgEventArgs e, FitDataCollector fitDataCollector)
        {
            // Нечего
        }
        static public void OnDeviceInfoMesg(MesgEventArgs e, FitDataCollector fitDataCollector)
        {
            // нечего
        }
        
        static public void OnActivityMesg(MesgEventArgs e, FitDataCollector fitDataCollector)
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

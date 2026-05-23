using Client.Core.Interfaces.DTO;
using EntityClassLibraryClient.Models;
using static Client.Core.Interfaces.DTO.FitDataCollector;
using static EntityClassLibraryClient.Models.WorkoutSkiing;

namespace Client.Infrastructure.Storage
{
    public class ConvertDtoToEntity
    {
        public static Workout FitDataCollectorToWorkout(FitDataCollector fitDataCollector)
        {
            Workout workout = fitDataCollector.SportType switch
            {
                SportTypes.Running => new WorkoutRunning() { AvgCadence = fitDataCollector.AvgCadence },
                SportTypes.Cycling => new WorkoutCycling() { AvgCadence = fitDataCollector.AvgCadence, AvgPower = fitDataCollector.AvgPower },
                SportTypes.Skiing => new WorkoutSkiing()
                {
                    StyleOfSkiing = fitDataCollector.StyleOfSkiing switch
                    {
                        SkiStyle.Skate => Styles.Skating,
                        SkiStyle.Classic => Styles.Classic,
                        SkiStyle.DoublePoling => Styles.DoublePoling,
                        _ => Styles.Classic,
                    }
                },
                _ => new WorkoutOther()
            };
            
            workout.SerialNumberDevice = fitDataCollector.DeviceData.SerialNumber;
            workout.TimeStart = (fitDataCollector.TimeStartUtc?.AddSeconds(fitDataCollector.OffsetInSeconds)) ?? DateTime.Now;
            workout.TimeEnd = (fitDataCollector.TimeEndUtc?.AddSeconds(fitDataCollector.OffsetInSeconds)) ?? DateTime.Now;
            workout.DistanceMeters = fitDataCollector.DistanceMeters;
            workout.AvgSpeedMetersPerHour = fitDataCollector.AvgSpeedMetersPerHour;
            workout.AvgHeartRate = fitDataCollector.AvgHeartRate;
            workout.ElevationGain = fitDataCollector.ElevationGain;
            workout.Description = null;


            workout.WorkoutSamples = fitDataCollector.RecordDataCollections.Select(record =>
            {
                var workOutSamples = RecordDataCollectionToWorkoutSamples(record, fitDataCollector.SportType ?? SportTypes.Other, fitDataCollector.OffsetInSeconds, workout.TimeStart);
                workOutSamples.Workout = workout;
                return workOutSamples;
            }).ToList();
            workout.Laps = fitDataCollector.LapDataCollections.Select(lap => 
            {
                var lapResult = LapDataCollectionToLap(lap, fitDataCollector.SportType ?? SportTypes.Other, fitDataCollector.OffsetInSeconds, workout.TimeStart);
                lapResult.Workout = workout;
                return lapResult;
            }).ToList();

            workout.SetIdentifier();

            return workout;
        }

        public static Lap LapDataCollectionToLap(LapDataCollection lapDataCollection, SportTypes type, int offsetInSeconds, DateTime startTimeWorkout)
        {
            Lap lap = type switch
            {
                SportTypes.Running => new LapRunning() { AvgCadence = lapDataCollection.AvgCadence },
                SportTypes.Cycling => new LapCycling() { AvgCadence = lapDataCollection.AvgCadence, AvgPower = lapDataCollection.AvgPower },
                _ => new LapOther()
            };

            lap.AvgHeartRate = lapDataCollection.AvgHeartRate;
            lap.AvgSpeedMetersPerHour = lapDataCollection.AvgSpeedMetersPerHour;
            lap.DistanceMeters = lapDataCollection.DistanceMeters;
            lap.ElevationGain = lapDataCollection.ElevationGain;
            lap.TimeEnd = (lapDataCollection.TimeEndUtc?.AddSeconds(offsetInSeconds)) ?? startTimeWorkout;

            return lap;
        }

        public static WorkoutSamples RecordDataCollectionToWorkoutSamples(RecordDataCollection recordDataCollection, SportTypes type, int offsetInSeconds, DateTime startTimeWorkout)
        {
            WorkoutSamples workoutSamples = type switch
            {
                SportTypes.Running => new WorkoutSamplesRunning() { Cadence = recordDataCollection.Cadence},
                SportTypes.Cycling => new WorkoutSamplesCycling() { Cadence = recordDataCollection.Cadence, Power = recordDataCollection.Power},
                _ => new WorkoutSamplesOther()
            };

            workoutSamples.Altitude = recordDataCollection.Altitude;
            workoutSamples.Latitude = recordDataCollection.Latitude;
            workoutSamples.Longitude = recordDataCollection.Longitude;
            workoutSamples.DistanceMeters = recordDataCollection.DistanceMeters;
            workoutSamples.SpeedMetersPerHour = recordDataCollection.SpeedMetersPerHour;
            workoutSamples.HeartRate = recordDataCollection.HeartRate;

            workoutSamples.TimeSeconds = recordDataCollection.TimeUtc.HasValue && recordDataCollection.TimeUtc.Value.AddSeconds(offsetInSeconds) >= startTimeWorkout
                ? (uint?)(recordDataCollection.TimeUtc.Value.AddSeconds(offsetInSeconds) - startTimeWorkout).TotalSeconds
                : 0;

            return workoutSamples;
        }
    }
}

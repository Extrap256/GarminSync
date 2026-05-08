using Client.Core.Interfaces.DTO;
using Client.Storage.Models;
using static Client.Core.Interfaces.DTO.FitDataCollector;
using static Client.Storage.Models.WorkoutSkiing;

namespace Client.Storage
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

            workout.TimeStart = fitDataCollector.TimeStart ?? DateTime.Now;
            workout.TimeEnd = fitDataCollector.TimeEnd ?? DateTime.Now;
            workout.DistanceMeters = fitDataCollector.DistanceMeters;
            workout.AvgSpeedMetersPerHour = fitDataCollector.AvgSpeedMetersPerHour;
            workout.AvgHeartRate = fitDataCollector.AvgHeartRate;
            workout.ElevationGain = fitDataCollector.ElevationGain;
            workout.Description = null;
            workout.WorkoutSamples = fitDataCollector.RecordDataCollections.Select(record =>
            {
                var workOutSamples = RecordDataCollectionToWorkoutSamples(record, fitDataCollector.SportType ?? SportTypes.Other, workout.TimeStart);
                workOutSamples.Workout = workout;
                return workOutSamples;
            }).ToList();
            workout.Laps = fitDataCollector.LapDataCollections.Select(lap => 
            {
                var lapResult = LapDataCollectionToLap(lap, fitDataCollector.SportType ?? SportTypes.Other, workout.TimeStart);
                lapResult.Workout = workout;
                return lapResult;
            }).ToList();

            return workout;
        }

        public static Lap LapDataCollectionToLap(LapDataCollection lapDataCollection, SportTypes type, DateTime startTimeWorkout)
        {
            Lap lap = type switch
            {
                SportTypes.Running => new LapRunning() { AvgCadence = lapDataCollection.AvgCadence },
                SportTypes.Cycling => new LapCycling() { AvgCadence = lapDataCollection.AvgCadence, AvgPower = lapDataCollection.AvgPower },
                _ => new LapOther()
            };

            lap.AvrHeartRate = lapDataCollection.AvgHeartRate;
            lap.AvrSpeedMetersPerHour = lapDataCollection.AvgSpeedMetersPerHour;
            lap.DistanceMeters = lapDataCollection.DistanceMeters;
            lap.ElevationGain = lapDataCollection.ElevationGain;
            lap.TimeStart = lapDataCollection.Time ?? startTimeWorkout;

            return lap;
        }

        public static WorkoutSamples RecordDataCollectionToWorkoutSamples(RecordDataCollection recordDataCollection, SportTypes type, DateTime startTimeWorkout)
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

            workoutSamples.TimeSeconds = recordDataCollection.Time.HasValue && recordDataCollection.Time.Value >= startTimeWorkout
                ? (uint?)(recordDataCollection.Time.Value - startTimeWorkout).TotalSeconds
                : 0;

            return workoutSamples;
        }
    }
}

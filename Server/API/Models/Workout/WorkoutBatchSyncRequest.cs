using System.ComponentModel.DataAnnotations;

namespace Server.API.Models.Workout
{
    public class WorkoutBatchSyncRequest : IValidatableObject
    {
        [Required]
        public List<WorkoutDto> Workouts { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Workouts == null || Workouts.Count == 0)
                yield return new ValidationResult("Список тренировок не может быть пустым", new[] { nameof(Workouts) });

            if (Workouts?.Count > 100)
                yield return new ValidationResult("Максимум 100 тренировок за один запрос", new[] { nameof(Workouts) });

            var dublicates = Workouts?.GroupBy(w => w.Identifier)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (dublicates?.Count > 0)
                yield return new ValidationResult($"Дубликаты Identifier в запросе: {string.Join(", ", dublicates)}", new[] { nameof(Workouts) });
        }
    }
}

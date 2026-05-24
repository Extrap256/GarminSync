using System;
using System.Collections.Generic;
using System.Text;

namespace Client.Core.Interfaces.DTO.API
{
    public record SyncWorkoutResponse(int WorkoutId, string Message);
}

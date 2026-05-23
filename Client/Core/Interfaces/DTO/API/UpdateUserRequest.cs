using System.Text.Json.Serialization;

namespace Client.Core.Interfaces.DTO.API
{
    public class UpdateUserRequest
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FirstName { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? LastName {  get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? YearOfBirth {  get; set; }

        public bool HasChanges => FirstName != null || LastName != null || YearOfBirth != null;
    }
}

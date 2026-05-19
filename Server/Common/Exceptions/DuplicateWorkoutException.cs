namespace Server.Common.Exceptions
{
    public class DuplicateWorkoutException : ApplicationException
    {
        public DuplicateWorkoutException(string message) : base(message) { }
    }
}

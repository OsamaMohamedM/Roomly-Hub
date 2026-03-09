namespace Application.Exceptions
{
    public class ValidationException : Exception
    {
        public IDictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>();

        public ValidationException()
        { }

        public ValidationException(string message) : base(message)
        {
        }
    }
}
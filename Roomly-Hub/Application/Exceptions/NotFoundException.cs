namespace Application.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string resourceName, object key)
            : base($"{resourceName} with key '{key}' was not found.")
        {
        }
    }
}
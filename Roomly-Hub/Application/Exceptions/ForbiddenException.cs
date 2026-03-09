namespace Application.Exceptions
{
    public class ForbiddenException : System.Exception
    {
        public ForbiddenException(string message) : base(message)
        {
        }

        public ForbiddenException()
       : base("You do not have permission to access this resource.")
        {
        }
    }
}
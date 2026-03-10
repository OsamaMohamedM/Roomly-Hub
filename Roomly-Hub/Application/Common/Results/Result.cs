namespace Application.Common.Results
{
    public class Result
    {
        protected Result(bool isSuccess, string? errorCode = null, string? errorMessage = null)
        {
            IsSuccess = isSuccess;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string? ErrorCode { get; }
        public string? ErrorMessage { get; }

        public static Result Success() => new(true);

        public static Result Failure(string errorCode, string errorMessage)
            => new(false, errorCode, errorMessage);
    }

    public class Result<T> : Result
    {
        private Result(bool isSuccess, T? value, string? errorCode = null, string? errorMessage = null)
            : base(isSuccess, errorCode, errorMessage)
        {
            Value = value;
        }

        public T? Value { get; }

        public static Result<T> Success(T value) => new(true, value);

        public static new Result<T> Failure(string errorCode, string errorMessage)
            => new(false, default, errorCode, errorMessage);
    }
}

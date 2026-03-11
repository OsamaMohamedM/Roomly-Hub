namespace Application.Common.Results
{
    public class Result
    {
        protected Result(
            bool isSuccess,
            string? errorCode = null,
            string? errorMessage = null,
            IReadOnlyDictionary<string, string[]>? errors = null)
        {
            IsSuccess = isSuccess;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            Errors = errors;
        }

        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string? ErrorCode { get; }
        public string? ErrorMessage { get; }
        public IReadOnlyDictionary<string, string[]>? Errors { get; }


        public static Result Success() => new(true);

        public static Result Failure(string errorCode, string errorMessage)
            => new(false, errorCode, errorMessage);

        public static Result Failure(string errorCode, string errorMessage, IReadOnlyDictionary<string, string[]> errors)
            => new(false, errorCode, errorMessage, errors);
    }

    public class Result<T> : Result
    {
        private Result(
            bool isSuccess,
            T? value,
            string? errorCode = null,
            string? errorMessage = null,
            IReadOnlyDictionary<string, string[]>? errors = null)
            : base(isSuccess, errorCode, errorMessage, errors)
        {
            Value = value;
        }

        public T? Value { get; }

        public static Result<T> Success(T value) => new(true, value);

        public static new Result<T> Failure(string errorCode, string errorMessage)
            => new(false, default, errorCode, errorMessage);

        public static Result<T> Failure(string errorCode, string errorMessage, IReadOnlyDictionary<string, string[]> errors)
            => new(false, default, errorCode, errorMessage, errors);
    }
}
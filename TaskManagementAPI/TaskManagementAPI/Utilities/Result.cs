namespace TaskManagementAPI.Utilities
{
    // A generic class to hold an outcome of an operation:
    // either the expected data (T) upon success, or error message upon failure.
    public class Result<T>
    {
        // The successfull data payload
        public T? Data { get; }
        public bool IsSuccess { get; }
        public string? Message { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;

        // Private constructors for success
        private Result(T data)
        {
            Data = data;
            IsSuccess = true;
        }
        private Result(bool success, T data, string message)
        {
            IsSuccess = success;
            Data = data;
            Message = message;
        }

        // Private constructor for failure
        private Result(string errorMessage)
        {
            Data = default;
            IsSuccess = false;
            Message = errorMessage;
        }

        // Static factory method for success
        public static Result<T> Success(T data) => new Result<T>(data);
        public static Result<T> Success(T data, string message) => new Result<T>(true, data, message);

        // Static factory method for failure
        public static Result<T> Failure (string errorMessage) => new Result<T>(errorMessage);
        public static Result<T> Failure(T data, string message) => new Result<T>(false, data, message);
    }

    // Update the Message property to be nullable to resolve CS8618
    public class Result
    {
        public bool IsSuccess { get; }
        public string? Message { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;

        // Private constructor for success
        private Result(bool success)
        {
            IsSuccess = success;
        }
        // Private constructor for failure
        private Result(bool success, string message)
        {
            IsSuccess = success;
            Message = message;
        }

        // Static factory method for success.
        public static Result Success() => new Result(true);

        public static Result Success(string message) => new Result(true, message);

        public static Result Failure() => new Result(false);
        public static Result Failure(string errorMessage) => new Result(false, errorMessage);
    }
}

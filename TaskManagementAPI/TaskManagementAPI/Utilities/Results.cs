namespace TaskManagementAPI.Utilities
{
    // A generic class to hold an outcome of an operation:
    // either the expected data (T) upon success, or error message upon failure.
    public class Result<T>
    {
        // The successfull data payload
        public T Data { get; }
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }

        // Private constructor for success
        private Result(T data)
        {
            Data = data;
            IsSuccess = true;
            ErrorMessage = null;
        }

        // Private constructor for failure
        private Result(string errorMessage)
        {
            Data = default;
            IsSuccess = false;
            ErrorMessage = errorMessage;
        }

        // Static factory method for success
        public static Result<T> Success(T data) => new Result<T>(data);

        // Static factory method for failure
        public static Result<T> Failure (string errorMessage) => new Result<T>(errorMessage);
    }

    // A non-generic version for operations that do not return data
    public class Result
    { 
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }

        // Private constructor for success
        private Result()
        {
            IsSuccess = true;
            ErrorMessage = null;
        }

        // Private constructor for failure
        private Result(string errorMessage)
        {
            IsSuccess = false;
            ErrorMessage = errorMessage;
        }

        // Static factory method for success.
        public static Result Success() => new Result();


        public static Result Failure(string errorMessage) => new Result(errorMessage);
    }
}

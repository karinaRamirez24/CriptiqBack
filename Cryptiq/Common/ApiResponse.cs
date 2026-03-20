namespace Cryptiq.Common
{
    public class ApiResponse<T>
    {
        public string Message { get; set; }
        public ResponseStatus Status { get; set; }
        public T Data { get; set; }

        public ApiResponse(string message, ResponseStatus status, T data = default)
        {
            Message = message;
            Status = status;
            Data = data;
        }
    }
}

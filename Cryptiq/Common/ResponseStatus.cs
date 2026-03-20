namespace Cryptiq.Common
{
    public enum ResponseStatus
    {
        Success,
        Created,
        NoContent,

        BadRequest,
        Unauthorized,
        Forbidden,
        NotFound,
        Conflict,

        ValidationError,
        Error
    }
}

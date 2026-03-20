namespace Cryptiq.Common
{
    public class Messages
    {
        public static class General
        {
            public const string Success = "Operation completed successfully";
            public const string Error = "Internal server error";
        }

        public static class Validation
        {
            public const string InvalidId = "Invalid ID";
            public const string Required = "Required fields are missing";
        }

        public static class Auth
        {
            public const string Unauthorized = "Unauthorized access";
            public const string Forbidden = "Access denied";
            public const string EmailNotRegistered = "Email is not registered";
        }
    }
}

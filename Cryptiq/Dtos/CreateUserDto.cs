using System.ComponentModel.DataAnnotations;

namespace CryptiqChat.Dtos
{
    public class CreateUserDto
    {
        [Required]
        public string UserName { get; set; }

        public string? LastName { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? Phone { get; set; }

        public string? ProfilePictureUrl { get; set; }
    }
}

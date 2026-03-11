using System.ComponentModel.DataAnnotations;

namespace CryptiqChat.Dtos
{
    public class PhoneVerificationDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [Phone]
        public string Phone { get; set; }
    }

}

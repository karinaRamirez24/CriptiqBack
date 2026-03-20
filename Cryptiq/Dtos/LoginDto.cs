using System.ComponentModel.DataAnnotations;

namespace Cryptiq.Dtos
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; }
    }

}

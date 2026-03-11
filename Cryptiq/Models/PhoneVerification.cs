using CryptiqChat.Models;

namespace Cryptiq.Models
{
    public class PhoneVerification
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string VerificationCode { get; set; }
        public DateTime ExpirationTime { get; set; }
        public bool IsVerified { get; set; }
        public int Attempts { get; set; }
        public DateTime CreatedAt { get; set; }

        // Relación con UseR
        public User User { get; set; }
    }

}

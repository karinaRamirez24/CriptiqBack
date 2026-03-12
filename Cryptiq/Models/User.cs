using Cryptiq.Models;
using System;

namespace CryptiqChat.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime DateOfRegistration { get; set; }
        public int StatusId { get; set; }
        public bool PhoneVerified { get; set; }
        public DateTime? InactivatedAt { get; set; }
      
        // Relación con PhoneVerifications
        public ICollection<PhoneVerification> PhoneVerifications { get; set; }
    }
}

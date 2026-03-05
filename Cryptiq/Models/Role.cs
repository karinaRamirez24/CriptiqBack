using System;
using System.Collections.Generic;

namespace CryptiqChat.Models
{
    public class Role
    {
        public Guid Id { get; set; }
        public string RoleName { get; set; }
        public int StatusId { get; set; }

        // Relación con UserRoles
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}

using System;
using System.Data;

namespace CryptiqChat.Models
{
    public class UserRole
    {
        public Guid Id { get; set; }

        // Claves foráneas
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }

        // Propiedades de navegación
        public User User { get; set; }
        public Role Role { get; set; }
    }
}

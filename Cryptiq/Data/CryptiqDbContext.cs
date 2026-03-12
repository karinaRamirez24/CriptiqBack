using Cryptiq.Models;
using CryptiqChat.Models;
using Microsoft.EntityFrameworkCore;


namespace CryptiqChat.Data
{
    public class CryptiqDbContext : DbContext
    {
        public CryptiqDbContext(DbContextOptions<CryptiqDbContext> options) : base(options) { }

        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<User> Users { get; set; }

        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<PhoneVerification> PhoneVerifications { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("USERS");

                entity.Property(u => u.UserName).HasColumnName("USER_NAME");
                entity.Property(u => u.LastName).HasColumnName("LAST_NAME");
                entity.Property(u => u.DateOfBirth).HasColumnName("DATE_OF_BIRTH");
                entity.Property(u => u.DateOfRegistration).HasColumnName("DATE_OF_REGISTRATION");
                entity.Property(u => u.ProfilePictureUrl).HasColumnName("PROFILE_PICTURE_URL");
                entity.Property(u => u.StatusId).HasColumnName("STATUS_ID");
                entity.Property(u => u.InactivatedAt).HasColumnName("INACTIVATED_AT");
                entity.Property(u => u.PhoneVerified).HasColumnName("PHONE_VERIFIED");
            });

            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.ToTable("CHAT_MESSAGE");

                entity.Property(m => m.SenderId).HasColumnName("SENDER_ID");
                entity.Property(m => m.ReceiverId).HasColumnName("RECEIVER_ID");
                entity.Property(m => m.GroupId).HasColumnName("GROUP_ID");
                entity.Property(m => m.EncryptedPayload).HasColumnName("ENCRYPTED_PAYLOAD");
                entity.Property(m => m.QrData).HasColumnName("QR_DATA");
                entity.Property(m => m.CreatedAt).HasColumnName("CREATED_AT");
                entity.Property(m => m.ExpiresAt).HasColumnName("EXPIRES_AT");
                entity.Property(m => m.IsDeleted).HasColumnName("IS_DELETED");
                entity.Property(m => m.DeletedAt).HasColumnName("DELETED_AT");
                entity.Property(m => m.StatusId).HasColumnName("STATUS_ID");
            });

            modelBuilder.Entity<UserRole>(entity =>
            { 
                entity.ToTable("USER_ROLES"); 
                entity.Property(ur => ur.UserId).HasColumnName("USER_ID"); 
                entity.Property(ur => ur.RoleId).HasColumnName("ROLE_ID"); 
                entity.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId); 
                entity.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId);
            });
            
            modelBuilder.Entity<Role>(entity => { entity.ToTable("ROLES"); entity.Property(r => r.RoleName).HasColumnName("ROLE_NAME"); entity.Property(r => r.StatusId).HasColumnName("STATUS_ID"); });

            modelBuilder.Entity<PhoneVerification>(entity =>
            {
                entity.ToTable("PHONE_VERIFICATIONS");

                entity.Property(pv => pv.UserId).HasColumnName("USER_ID");
                entity.Property(pv => pv.VerificationCode).HasColumnName("VERIFICATION_CODE");
                entity.Property(pv => pv.ExpirationTime).HasColumnName("EXPIRATION_TIME");
                entity.Property(pv => pv.IsVerified).HasColumnName("IS_VERIFIED");
                entity.Property(pv => pv.Attempts).HasColumnName("ATTEMPTS");
                entity.Property(pv => pv.CreatedAt).HasColumnName("CREATED_AT");

                entity.HasOne(pv => pv.User)
                      .WithMany(u => u.PhoneVerifications)
                      .HasForeignKey(pv => pv.UserId);
            });
        }

    }
}

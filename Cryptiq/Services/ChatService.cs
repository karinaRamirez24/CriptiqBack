using Cryptiq.Models;
using CryptiqChat.Data;
using CryptiqChat.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptiqChat.Services
{
    public class ChatService
    {
        private readonly CryptiqDbContext _db;

        public ChatService(CryptiqDbContext db)
        {
            _db = db;
            Console.WriteLine($"Connected to {_db.Database.GetConnectionString()}");
        }

        // --------------------------------USUARIOS--------------------------------
        // ── Crear usuario ───────────────────────────────
        // ── Crear usuario con validación de teléfono ───────────────────────────────
        public async Task<bool> AddUserAsync(User user)
        {
            // Verificar si ya existe un usuario con ese número de teléfono
            bool phoneExists = await _db.Users.AnyAsync(u => u.Phone == user.Phone);

            if (phoneExists)
            {
                // No permitir la creación
                return false;
            }

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return true;
        }


        // ── Obtener usuario por Id ───────────────────────────────
        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        // Guardar código de verificación
        public async Task SavePhoneVerificationAsync(Guid userId, string code, DateTime expiration)
        {
            var verification = new PhoneVerification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                VerificationCode = code,
                ExpirationTime = expiration,
                IsVerified = false,
                Attempts = 0,
                CreatedAt = DateTime.UtcNow
            };

            _db.PhoneVerifications.Add(verification);
            await _db.SaveChangesAsync();
        }

        // Validar código
        public async Task<bool> ValidateCodeAsync(Guid userId, string code)
        {
            var verification = await _db.PhoneVerifications
                .Where(v => v.UserId == userId && !v.IsVerified)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();

            if (verification == null)
                return false;

            verification.Attempts++;
            _db.PhoneVerifications.Update(verification);
            await _db.SaveChangesAsync();

            if (verification.Attempts > 3)
                return false; // demasiados intentos

            if (verification.ExpirationTime < DateTime.UtcNow)
                return false;

            if (verification.VerificationCode != code)
                return false;

            return true;
        }


        // Marcar teléfono como verificado
        public async Task MarkPhoneAsVerifiedAsync(Guid userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                user.PhoneVerified = true;
                _db.Users.Update(user);
            }

            var verification = await _db.PhoneVerifications
                .Where(v => v.UserId == userId && !v.IsVerified)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();

            if (verification != null)
            {
                verification.IsVerified = true;
                _db.PhoneVerifications.Update(verification);
            }

            await _db.SaveChangesAsync();
        }

        // ----- Eliminar usuario ───────────────────────────────
        public async Task<bool> SoftDeleteUserAsync(Guid userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.StatusId = 2;
            await _db.SaveChangesAsync();
            return true;
        }

        // ── UPDATE USER ───────────────────────────────
        public async Task<bool> UpdateUserAsync(Guid userId, User updatedUser)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return false;

            // Actualiza los campos que quieras permitir
            user.UserName = updatedUser.UserName;
            user.LastName = updatedUser.LastName;
            user.DateOfBirth = updatedUser.DateOfBirth;
            user.ProfilePictureUrl = updatedUser.ProfilePictureUrl;
            user.StatusId = updatedUser.StatusId;

            await _db.SaveChangesAsync();
            return true;
        }

        // ── Validar si un usuario existe ───────────────────────────
        public async Task<bool> UserExistsAsync(Guid userId)
        {
            return await _db.Users.AnyAsync(u => u.Id == userId);
        }

        // --------------------------------MENSAJES--------------------------------
        // ── Guardar mensaje 1-a-1 con estado ───────────────────────
        public async Task<ChatMessage> SavePrivateMessageAsync(
            Guid senderId, Guid receiverId,
            string encryptedPayload, string qrData,
            int statusId = 4)
        {
            var message = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                EncryptedPayload = encryptedPayload,
                QrData = qrData,
                CreatedAt = DateTime.UtcNow,
                StatusId = statusId,
                IsDeleted = false
            };

            _db.ChatMessages.Add(message);
            await _db.SaveChangesAsync();

            Console.WriteLine($"Insertado en BD: {message.Id}, Status={message.StatusId}");
            return message;
        }

        // ── Guardar mensaje grupal ─────────────────────────────────
        public async Task<ChatMessage> SaveGroupMessageAsync(
            Guid senderId, Guid groupId,
            string encryptedPayload, string qrData)
        {
            var message = new ChatMessage
            {
                SenderId = senderId,
                GroupId = groupId,
                EncryptedPayload = encryptedPayload,
                QrData = qrData,
                CreatedAt = DateTime.UtcNow
            };

            _db.ChatMessages.Add(message);
            await _db.SaveChangesAsync();
            return message;
        }

        // ── Obtener mensaje por Id ─────────────────────────────────
        public async Task<ChatMessage?> GetMessageByIdAsync(Guid messageId)
        {
            return await _db.ChatMessages
                .Include(m => m.Status)
                .FirstOrDefaultAsync(m => m.Id == messageId);
        }

        // ── Historial privado ──────────────────────────────────────
        public async Task<List<ChatMessage>> GetPrivateHistoryAsync(
            Guid userId, Guid contactId, int limit = 50)
        {
            return await _db.ChatMessages
                .Where(m =>
                    m.IsDeleted == false &&
                    m.GroupId == null &&
                    ((m.SenderId == userId && m.ReceiverId == contactId) ||
                     (m.SenderId == contactId && m.ReceiverId == userId)))
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .Include(m => m.Sender)
                .ToListAsync();
        }

        // ── Historial grupal ───────────────────────────────────────
        public async Task<List<ChatMessage>> GetGroupHistoryAsync(
            Guid groupId, int limit = 50)
        {
            return await _db.ChatMessages
                .Where(m => m.GroupId == groupId && m.IsDeleted == false)
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .Include(m => m.Sender)
                .ToListAsync();
        }

        // ── Obtener mensajes por estado ────────────────────────────
        public async Task<List<ChatMessage>> GetMessagesByStatusAsync(Guid receiverId, int statusId)
        {
            return await _db.ChatMessages
                .Where(m => m.ReceiverId == receiverId && m.StatusId == statusId && m.IsDeleted == false)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        // ── Actualizar estado ──────────────────────────────────────
        public async Task UpdateMessageStatusAsync(Guid messageId, int newStatusId)
        {
            var message = await _db.ChatMessages.FindAsync(messageId);
            if (message != null)
            {
                message.StatusId = newStatusId;
                await _db.SaveChangesAsync();
            }
        }

        // ── Eliminar físicamente ───────────────────────────────────
        public async Task<bool> HardDeleteMessageAsync(Guid messageId)
        {
            var message = await _db.ChatMessages.FindAsync(messageId);
            if (message == null) return false;

            _db.ChatMessages.Remove(message);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}

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
            Console.WriteLine($"Conectado a: {_db.Database.GetConnectionString()}");
        }

        // --------------------------------USUARIOS--------------------------------
        // ── Crear usuario ───────────────────────────────
        public async Task AddUserAsync(User user)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        // ── Obtener usuario por Id ───────────────────────────────
        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }
        // ----- Eliminar usuario ───────────────────────────────
        public async Task<bool> SoftDeleteUserAsync(Guid userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.StatusId = 2;
            user.LastLogin = DateTime.UtcNow; 
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

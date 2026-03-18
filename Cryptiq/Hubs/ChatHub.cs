
using CryptiqChat.Dtos;
using CryptiqChat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CryptiqChat.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private static readonly Dictionary<string, string> _connectedUsers = new();
        private readonly ChatService _chatService;
        private readonly PresenceService _presenceService;

        public ChatHub(ChatService chatService, PresenceService presenceService)
        {
            _chatService = chatService;
            _presenceService = presenceService;
        }

        // ── Al conectarse ───────────────────────────────────────────
        public override async Task OnConnectedAsync()
        {
            // ✅ Leer userId desde el JWT (claim "sub") — es la fuente de verdad
            var userIdFromJwt = Context.User?.FindFirst("sub")?.Value
                             ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdFromJwt) || !Guid.TryParse(userIdFromJwt, out var userGuid))
            {
                await Clients.Caller.SendAsync("ErrorMessage", new
                {
                    Code = "INVALID_TOKEN",
                    Message = "Token inválido o sin userId."
                });
                Context.Abort();
                return;
            }

            // ✅ Si el cliente también manda userId por query string, verificar que coincida
            var userIdFromQuery = Context.GetHttpContext()?.Request.Query["userId"].ToString();
            if (!string.IsNullOrEmpty(userIdFromQuery))
            {
                // Comparar como GUID (case-insensitive) en lugar de como string
                if (!Guid.TryParse(userIdFromQuery, out var queryGuid) || queryGuid != userGuid)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", new
                    {
                        Code = "USERID_MISMATCH",
                        Message = "El userId no coincide con el token."
                    });
                    Context.Abort();
                    return;
                }
            }

            // A partir de aquí usa siempre userGuid (del JWT), nunca el query string
            var userExists = await _chatService.UserExistsAsync(userGuid);
            if (!userExists)
            {
                await Clients.Caller.SendAsync("ErrorMessage", new
                {
                    Code = "USER_NOT_FOUND",
                    Message = "Usuario no encontrado."
                });
                Context.Abort();
                return;
            }

            await _presenceService.RegisterConnectionAsync(userGuid, Context.ConnectionId);
            await Clients.Caller.SendAsync("ConnectedOk", new { UserId = userIdFromJwt });

            var pending = await _chatService.GetMessagesByStatusAsync(userGuid, 4);
            foreach (var msg in pending)
            {
                await Clients.Caller.SendAsync("ReceivePrivateMessage", new
                {
                    msg.Id,
                    SenderId = msg.SenderId.ToString(),
                    ReceiverId = msg.ReceiverId?.ToString(),
                    Payload = msg.EncryptedPayload,
                    msg.QrData,
                    msg.CreatedAt,
                    msg.StatusId
                });
                await _chatService.UpdateMessageStatusAsync(msg.Id, 1);
            }

            await base.OnConnectedAsync();
        }

        // ── Al desconectarse ────────────────────────────────────────

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userIdStr = Context.GetHttpContext()?.Request.Query["userId"].ToString();

            if (Guid.TryParse(userIdStr, out var userGuid))
            {
                // 👉 Aquí eliminas la conexión de Redis
                await _presenceService.RemoveConnectionAsync(userGuid);
                Console.WriteLine($"❌ Offline: {userGuid}");
            }

            await base.OnDisconnectedAsync(exception);
        }


        // ── Mensaje privado + guardado en BD ──────────────────────
        public async Task SendPrivateMessage(string receiverId, string encryptedPayload, string qrData)
        {
            // Quién envía → desde Redis
            var senderIdStr = Context.User?.FindFirst("sub")?.Value
                   ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(senderIdStr) || !Guid.TryParse(senderIdStr, out var senderId))
            {
                await Clients.Caller.SendAsync("ErrorMessage", new { Code = "UNAUTHORIZED" });
                return;
            }

            if (!Guid.TryParse(receiverId, out var receiverGuid))
            {
                await Clients.Caller.SendAsync("ErrorMessage", new { Code = "INVALID_RECEIVER" });
                return;
            }

            if (senderId == receiverGuid)
            {
                await Clients.Caller.SendAsync("ErrorMessage", new { Code = "SELF_MESSAGE" });
                return;
            }
            // Verificar que el usuario receptor existe
            var canSend = await _chatService.CanSendMessageAsync(senderId, receiverGuid);
            if (!canSend)
            {
                await Clients.Caller.SendAsync("ErrorMessage", new
                {
                    Code = "CANNOT_SEND",
                    Message = "No puedes enviar mensajes a este usuario."
                });
                return;
            }
            // ¿Está online el receptor?
            var receiverConnId = await _presenceService.GetConnectionAsync(receiverGuid);
            var statusId = receiverConnId != null ? 1 : 4; // entregado o pendiente

            var saved = await _chatService.SavePrivateMessageAsync(
                senderId, receiverGuid, encryptedPayload, qrData, statusId);

            var dto = new ChatMessageDto
            {
                Id = saved.Id,
                SenderId = senderId,
                ReceiverId = receiverGuid,
                Payload = encryptedPayload,
                QrData = qrData,
                CreatedAt = saved.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                StatusId = saved.StatusId
            };

            // Entregar si está conectado
            if (!string.IsNullOrEmpty(receiverConnId))
                await Clients.Client(receiverConnId).SendAsync("ReceivePrivateMessage", dto);

            await Clients.Caller.SendAsync("MessageSent", dto);

            // Renovar presencia del sender
            await _presenceService.RefreshPresenceAsync(senderId);
        }



        // ── Mensaje grupal + guardado en BD ───────────────────────
        public async Task SendGroupMessage(string groupId, string encryptedPayload, string qrData)
        {
            var senderIdStr = _connectedUsers.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            if (string.IsNullOrEmpty(senderIdStr)) return;

            var senderId = Guid.Parse(senderIdStr);
            var groupGuid = Guid.Parse(groupId);

            // 1. Guardar en BD
            var saved = await _chatService.SaveGroupMessageAsync(senderId, groupGuid, encryptedPayload, qrData);

            var message = new
            {
                Id = saved.Id,
                SenderId = senderIdStr,
                GroupId = groupId,
                Payload = encryptedPayload,
                QrData = qrData,
                CreatedAt = saved.CreatedAt
            };

            // 2. Entregar a todos en el grupo
            await Clients.Group(groupId).SendAsync("ReceiveGroupMessage", message);

            Console.WriteLine($"💾 Group message saved in database: {saved.Id}");
        }

        public async Task JoinGroup(string groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
            Console.WriteLine($"➕ {Context.ConnectionId} joined the group {groupId}");
        }

        public async Task LeaveGroup(string groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        }

        public async Task MarkMessageAsRead(Guid messageId)
        {
            await _chatService.UpdateMessageStatusAsync(messageId, 2); // Read
            var message = await _chatService.GetMessageByIdAsync(messageId);

            if (message != null)
            {
                if (_connectedUsers.TryGetValue(message.SenderId.ToString(), out var senderConnectionId))
                {
                    await Clients.Client(senderConnectionId).SendAsync("MessageReadConfirmation", new
                    {
                        Id = message.Id,
                        StatusId = message.StatusId,
                        ReadAt = DateTime.UtcNow
                    });
                }
            }

            Console.WriteLine($"👁️ Message read: {messageId}");
        }

        // ── Eliminar mensaje 1-a-1 (soft delete) ─────────────────────
        public async Task DeleteMessage(Guid messageId)
        {
            var success = await _chatService.HardDeleteMessageAsync(messageId);

            if (success)
            {
                await Clients.Caller.SendAsync("MessageDeleted", new
                {
                    Id = messageId,
                    DeletedAt = DateTime.UtcNow
                });

                // Notificar al receptor si está conectado
                var message = await _chatService.GetMessageByIdAsync(messageId);
                if (message?.ReceiverId != null &&
                    _connectedUsers.TryGetValue(message.ReceiverId.ToString(), out var receiverConnectionId))
                {
                    await Clients.Client(receiverConnectionId).SendAsync("MessageDeleted", new
                    {
                        Id = messageId,
                        DeletedAt = DateTime.UtcNow
                    });
                }
            }
            else
            {
                await Clients.Caller.SendAsync("ErrorMessage", new
                {
                    Code = "DELETE_FAILED",
                    Message = "Message not found or could not be deleted."
                });
            }
        }



    }
}

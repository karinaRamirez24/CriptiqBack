using CryptiqChat.Dtos;
using CryptiqChat.Services;
using Microsoft.AspNetCore.SignalR;

namespace CryptiqChat.Hubs
{
    public class ChatHub : Hub
    {
        private static readonly Dictionary<string, string> _connectedUsers = new();
        private readonly ChatService _chatService;

        public ChatHub(ChatService chatService)
        {
            _chatService = chatService;
        }

        // ── Al conectarse ───────────────────────────────────────────
        public override async Task OnConnectedAsync()
        {
            var userIdStr = Context.GetHttpContext()?.Request.Query["userId"].ToString();

            // Validar que no esté vacío
            if (string.IsNullOrEmpty(userIdStr))
            {
                Console.WriteLine("⚠️ Conexión rechazada: userId vacío");
                await Clients.Caller.SendAsync("ErrorMessage", new
                {
                    Code = "INVALID_USERID",
                    Message = "You must provide a valid userId (GUID)."
                });
                Context.Abort();
                return;
            }

            // Validar que sea un GUID válido
            if (!Guid.TryParse(userIdStr, out var userGuid))
            {
                Console.WriteLine($"⚠️ Conexión rechazada: userId inválido ({userIdStr})");
                await Clients.Caller.SendAsync("ErrorMessage", new
                {
                    Code = "INVALID_USERID",
                    Message = "The provided userId is not a valid GUID."
                });
                Context.Abort();
                return;
            }

            // (Opcional) Validar que el GUID exista en la tabla USERS
            var userExists = await _chatService.UserExistsAsync(userGuid);
            if (!userExists)
            {
                Console.WriteLine($"⚠️ Conexión rechazada: userId no existe en BD ({userIdStr})");
                await Clients.Caller.SendAsync("ErrorMessage", new
                {
                    Code = "USER_NOT_FOUND",
                    Message = "The provided userId does not exist in the system."
                });
                Context.Abort();
                return;
            }

            // Si es válido y existe, registrar
            _connectedUsers[userIdStr] = Context.ConnectionId;
            Console.WriteLine($"✅ User logged in: {userIdStr}");

            // Confirmar conexión válida al cliente
            await Clients.Caller.SendAsync("ConnectedOk", new
            {
                UserId = userIdStr
            });

            // Buscar mensajes pendientes
            var pendingMessages = await _chatService.GetMessagesByStatusAsync(userGuid, 4);

            foreach (var msg in pendingMessages)
            {
                var message = new
                {
                    Id = msg.Id,
                    SenderId = msg.SenderId.ToString(),
                    ReceiverId = msg.ReceiverId?.ToString(),
                    Payload = msg.EncryptedPayload,
                    QrData = msg.QrData,
                    CreatedAt = msg.CreatedAt,
                    StatusId = msg.StatusId
                };

                await Clients.Caller.SendAsync("ReceivePrivateMessage", message);

                // Actualizar estado a entregado
                await _chatService.UpdateMessageStatusAsync(msg.Id, 1);
            }

            await base.OnConnectedAsync();
        }


        // ── Al desconectarse ────────────────────────────────────────
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var user = _connectedUsers.FirstOrDefault(x => x.Value == Context.ConnectionId);
            if (user.Key != null)
            {
                _connectedUsers.Remove(user.Key);
                Console.WriteLine($"❌ Offline: {user.Key}");
            }
            await base.OnDisconnectedAsync(exception);
        }

        // ── Mensaje privado + guardado en BD ──────────────────────
        public async Task SendPrivateMessage(string receiverId, string encryptedPayload, string qrData)
        {
            try
            {
                var senderIdStr = _connectedUsers.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
                if (string.IsNullOrEmpty(senderIdStr))
                {
                    Console.WriteLine("⚠️ Sender not registered");
                    return;
                }

                var senderId = Guid.Parse(senderIdStr);
                var receiverGuid = Guid.Parse(receiverId);

                if (senderId == receiverGuid)
                {
                    Console.WriteLine($"⚠️ User {senderId} tried to send a message to themselves.");
                    await Clients.Caller.SendAsync("ErrorMessage", new
                    {
                        Code = "SELF_MESSAGE",
                        Message = "You cannot send a private message to yourself."
                    });
                    return;
                }

                // Si el receptor está conectado → entregado (1), si no → pendiente (4)
                var statusId = _connectedUsers.ContainsKey(receiverId) ? 1 : 4;

                var saved = await _chatService.SavePrivateMessageAsync(
                    senderId, receiverGuid, encryptedPayload, qrData, statusId);

                // Construir DTO
                var messageDto = new ChatMessageDto
                {
                    Id = saved.Id,
                    SenderId = senderId,
                    ReceiverId = receiverGuid,
                    Payload = encryptedPayload,
                    QrData = qrData,
                    CreatedAt = saved.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    StatusId = saved.StatusId
                };

                // Enviar al receptor si está conectado
                if (_connectedUsers.TryGetValue(receiverId, out var receiverConnectionId))
                {
                    await Clients.Client(receiverConnectionId).SendAsync("ReceivePrivateMessage", messageDto);
                    await _chatService.UpdateMessageStatusAsync(saved.Id, 1); // entregado
                }

                // Confirmar al remitente
                await Clients.Caller.SendAsync("MessageSent", messageDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en SendPrivateMessage: {ex}");
                throw; // deja que SignalR lo propague
            }
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

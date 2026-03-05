using CryptiqChat.Dtos;
using CryptiqChat.Services;
using Microsoft.AspNetCore.Mvc;

namespace CryptiqChatWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly ChatService _chatService;

        public MessagesController(ChatService chatService)
        {
            _chatService = chatService;
        }
        // GET api/messages/{userId}/{contactId}
        [HttpGet("{userId}/{contactId}")]
        public async Task<ActionResult<IEnumerable<ChatMessageDto>>> GetPrivateHistory(Guid userId, Guid contactId)
        {
            var messages = await _chatService.GetPrivateHistoryAsync(userId, contactId);

            var dtos = messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                GroupId = m.GroupId,
                Payload = m.EncryptedPayload,
                QrData = m.QrData,
                CreatedAt = m.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                StatusId = m.StatusId
            });

            return Ok(dtos);
        }

        // GET api/messages/group/{groupId}
        // Devuelve el historial de mensajes de un grupo
        [HttpGet("group/{groupId}")]
        public async Task<IActionResult> GetGroupHistory(Guid groupId)
        {
            var history = await _chatService.GetGroupHistoryAsync(groupId);
            return Ok(history);
        }

        // GET api/messages/{messageId}
        // Devuelve un mensaje específico por Id
        [HttpGet("{messageId:guid}")]
        public async Task<IActionResult> GetMessage(Guid messageId)
        {
            var message = await _chatService.GetMessageByIdAsync(messageId);
            if (message == null) return NotFound(new { Message = "Message not found" });
            return Ok(message);
        }

        // DELETE api/messages/{messageId}
        // Elimina físicamente un mensaje
        [HttpDelete("{messageId:guid}")]
        public async Task<IActionResult> DeleteMessage(Guid messageId)
        {
            var success = await _chatService.HardDeleteMessageAsync(messageId);
            if (!success) return NotFound(new { Message = "Message not found" });
            return NoContent();
        }
    }
}

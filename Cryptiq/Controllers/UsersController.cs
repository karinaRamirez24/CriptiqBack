using CryptiqChat.Dtos;
using CryptiqChat.Models;
using CryptiqChat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cryptiq.Dtos; 

namespace CryptiqChatWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ChatService _chatService;
        private readonly SmsService _smsService;

        public UsersController(ChatService chatService, SmsService smsService)
        {
            _chatService = chatService;
            _smsService = smsService;
        }

        // CREATE USER - POST api/users 
      
        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = dto.UserName,
                LastName = dto.LastName,
                Email = dto.Email,
                Phone = dto.Phone,
                ProfilePictureUrl = dto.ProfilePictureUrl,
                DateOfBirth = dto.DateOfBirth,
                DateOfRegistration = DateTime.UtcNow,
                StatusId = 4,
                PhoneVerified = false

            };

            var created = await _chatService.AddUserAsync(user);

            if (!created)
            {
                return BadRequest("The phone number is already registered.");
            }

            var result = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                ProfilePictureUrl = user.ProfilePictureUrl
            };
            return CreatedAtAction(nameof(GetUser), new { userId = user.Id }, result);
        }


        // UPDATE USER - PUT api/users/{userId}
        // [Authorize]
        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserDto dto)
        {
            var user = await _chatService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound(new { Message = $"User {userId} not found." });

            // Solo actualiza si el campo viene en el DTO
            if (!string.IsNullOrEmpty(dto.UserName))
                user.UserName = dto.UserName;

            if (!string.IsNullOrEmpty(dto.LastName))
                user.LastName = dto.LastName;

            if (dto.DateOfBirth != default) 
                user.DateOfBirth = dto.DateOfBirth;

            if (!string.IsNullOrEmpty(dto.ProfilePictureUrl))
                user.ProfilePictureUrl = dto.ProfilePictureUrl;

            await _chatService.UpdateUserAsync(userId, user);
            return NoContent();
        }




        // SEND VERIFICATION CODE - POST api/users/{userId}/sendVerificationCode
        [HttpPost("{userId}/sendVerificationCode")]
        public async Task<IActionResult> SendVerificationCode(Guid userId)
        {
            var user = await _chatService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound(new { Message = $"User {userId} not found." });

            var code = new Random().Next(100000, 999999).ToString();
            var expiration = DateTime.UtcNow.AddMinutes(5);

            await _chatService.SavePhoneVerificationAsync(userId, code, expiration);

            // Normalizar a formato E.164
            var toPhone = user.Phone.StartsWith("+")
                ? user.Phone
                : $"+52{user.Phone}";

            await _smsService.SendSmsAsync(toPhone, $"Your verification code is: {code}");

            return Ok(new { Message = "Code sent by SMS" });
        }

        // Verificacion de codigo - POST api/users/{userId}/verifyCode
        [HttpPost("{userId}/verifyCode")]
        public async Task<IActionResult> VerifyCode(Guid userId, [FromBody] SmsCodeDto dto)
        {
            var user = await _chatService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound(new { Message = $"User {userId} not found." });

            // Usar el servicio para validar
            var isValid = await _chatService.ValidateCodeAsync(userId, dto.Code);

            if (!isValid)
                return BadRequest(new { Message = "Invalid or expired code." });

            // Si es válido, marcar como verificado
            await _chatService.MarkPhoneAsVerifiedAsync(userId);

            // Actualizar estado del usuario
            user.StatusId = 6; // Ejemplo: SmsVerified
            await _chatService.UpdateUserAsync(userId, user);

            return Ok(new { Message = "Phone number verified successfully." });
        }

        // DELETE USER - DELETE api/users/{userId}
       
        [HttpDelete("{userId}")]
        public async Task<IActionResult> SoftDeleteUser(Guid userId)
        {
            var deleted = await _chatService.SoftDeleteUserAsync(userId);

            if (!deleted)
                return NotFound(new { Message = $"User {userId} not found." });

            return NoContent(); // 204
        }


        // GET api/users/{userId}
        [HttpGet("{userId}")]
        public async Task<ActionResult<UserDto>> GetUser(Guid userId)
        {
            var user = await _chatService.GetUserByIdAsync(userId);

            if (user == null)
                return NotFound();

            var dto = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                ProfilePictureUrl = user.ProfilePictureUrl
            };

            return Ok(dto);
        }




    }
}

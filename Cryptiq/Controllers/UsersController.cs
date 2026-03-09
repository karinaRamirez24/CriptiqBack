using CryptiqChat.Dtos;
using CryptiqChat.Models;
using CryptiqChat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CryptiqChatWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ChatService _chatService;

        public UsersController(ChatService chatService)
        {
            _chatService = chatService;
        }

        // CREATE USER - POST api/users 
        [Authorize]
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
                DateOfRegistration = DateTime.UtcNow,
                StatusId = 1

            };

            await _chatService.AddUserAsync(user);

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
        [Authorize]
        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] User updatedUser)
        {
            var result = await _chatService.UpdateUserAsync(userId, updatedUser);

            if (!result)
                return NotFound(new { Message = $"User {userId} not found." });

            return NoContent(); // 204 si se actualizó correctamente
        }

        // DELETE USER - DELETE api/users/{userId}
        [Authorize]
        [HttpDelete("{userId}")]
        public async Task<IActionResult> SoftDeleteUser(Guid userId)
        {
            var deleted = await _chatService.SoftDeleteUserAsync(userId);

            if (!deleted)
                return NotFound(new { Message = $"User {userId} not found." });

            return NoContent(); // 204
        }

        // GET api/users/{userId}/exists
        [HttpGet("{userId}/exists")]
        public async Task<IActionResult> UserExists(Guid userId)
        {
            var exists = await _chatService.UserExistsAsync(userId);
            return Ok(new { UserId = userId, Exists = exists });
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

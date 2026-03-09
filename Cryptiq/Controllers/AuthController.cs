using CryptiqChat.Data;
using Microsoft.AspNetCore.Mvc;
using Cryptiq.Dtos;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly CryptiqDbContext _db;
    private readonly JwtService _jwtService;

    public AuthController(CryptiqDbContext db, JwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto loginDto)
    {
        var email = loginDto.Email.ToLower().Trim();

        var user = _db.Users.FirstOrDefault(u => u.Email.ToLower() == email);
        if (user == null)
            return Unauthorized(new { Message = "Email no registrado" });

        var token = _jwtService.GenerateJwtToken(user);
        return Ok(new { Token = token });
    }

}

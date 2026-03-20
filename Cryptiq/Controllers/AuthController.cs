using Cryptiq.Common;
using Cryptiq.Controllers;
using Cryptiq.Dtos;
using CryptiqChat.Data;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseController
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
         return NotFoundResponse(Messages.Auth.EmailNotRegistered);

        var token = _jwtService.GenerateJwtToken(user);
        return Ok(new { Token = token });
    }

}

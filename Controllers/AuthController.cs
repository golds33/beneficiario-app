using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pension65Api.Data;
using Pension65Api.Models;
using Pension65Api.DTOs;
using Pension65Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace Pension65Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(AppDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AuthRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("El nombre de usuario y la contraseña son requeridos.");

            if (await _context.Usuarios.AnyAsync(u => u.Username == req.Username))
                return BadRequest("El nombre de usuario ya existe.");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);

            var user = new Usuario
            {
                Username = req.Username,
                PasswordHash = passwordHash,
                Role = "Admin"
            };

            _context.Usuarios.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Usuario registrado correctamente." });
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] AuthRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("Usuario y contraseña requeridos.");

            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == req.Username);
            if (user == null)
                return Unauthorized("Usuario no encontrado.");

            var passwordValid = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
            if (!passwordValid)
                return Unauthorized("Contraseña incorrecta.");

            var token = _jwtService.GenerateToken(user);

            return Ok(new AuthResponse
            {
                Token = token,
                Username = user.Username,
                Role = user.Role,
                Expiration = DateTime.UtcNow.AddMinutes(60)
            });
        }

       [Authorize]
        [HttpGet("me")]
        public ActionResult<UsuarioDto> GetCurrentUser()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = _context.Usuarios.FirstOrDefault(u => u.Username == username);
            if (user == null)
                return NotFound();

            return new UsuarioDto
            {
                Id = user.Id,
                Username = user.Username,
                NombreCompleto = user.NombreCompleto,
                Role = user.Role,
                FechaCreacion = user.FechaCreacion
            };
        }

    }
}

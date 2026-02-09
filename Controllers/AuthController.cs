using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WishListAPI.Data;
using WishListAPI.DTOs;
using WishListAPI.Models;
using WishListAPI.Services;

namespace WishListAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuthService _authService;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AuthController(AppDbContext context, AuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!Enum.TryParse<UserRole>(model.Role, true, out var userRole))
            {
                return BadRequest(new { message = "Role invalida." });
            }

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                return Conflict(new { message = "E-mail ja cadastrado." });
            }

            if (userRole == UserRole.Namorado)
            {
                if (string.IsNullOrWhiteSpace(model.CodigoNamorada))
                {
                    return BadRequest(new { message = "O codigo da namorada e obrigatorio para o namorado!" });
                }

                var namoradaExiste = await _context.Users.AnyAsync(u =>
                    u.Role == UserRole.Namorada.ToString() && u.CodigoNamorada == model.CodigoNamorada);

                if (!namoradaExiste)
                {
                    return BadRequest(new { message = "Codigo da namorada invalido." });
                }
            }

            var user = new User
            {
                Nome = model.Nome,
                Email = model.Email,
                Role = userRole.ToString(),
                CodigoNamorada = model.CodigoNamorada,
                DataCriacao = DateTime.Now
            };

            user.SenhaHash = _passwordHasher.HashPassword(user, model.Senha);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Usuario criado com sucesso!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                return Unauthorized("E-mail ou senha invalidos");
            }

            var verify = _passwordHasher.VerifyHashedPassword(user, user.SenhaHash, model.Senha);
            if (verify == PasswordVerificationResult.Failed)
            {
                return Unauthorized("E-mail ou senha invalidos");
            }

            var token = _authService.GenerateJwtToken(user);

            return Ok(new LoginResponseDto
            {
                Token = token,
                Nome = user.Nome,
                Email = user.Email,
                Role = user.Role
            });
        }
    }
}

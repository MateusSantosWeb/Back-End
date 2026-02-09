using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WishListAPI.Data;
using WishListAPI.Models;

namespace WishListAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Namorado")] // So o namorado acessa
    public class ProgressController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProgressController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/progress - Lista todos os wishes com status secreto
        [HttpGet]
        public async Task<IActionResult> GetAllProgress()
        {
            var progresses = await _context.WishProgresses
                .Include(p => p.Wish)
                .ThenInclude(w => w.Usuario)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return Ok(progresses);
        }

        // PUT: api/progress/5/iniciar - Marca "Vou Realizar"
        [HttpPut("{wishId}/iniciar")]
        public async Task<IActionResult> IniciarRealizacao(int wishId)
        {
            var progress = await _context.WishProgresses
                .FirstOrDefaultAsync(p => p.WishId == wishId);

            if (progress == null)
            {
                return NotFound(new { message = "Wish nao encontrado" });
            }

            progress.StatusSecreto = StatusSecreto.Vou_Realizar.ToString();

            await _context.SaveChangesAsync();

            return Ok(new { message = "Marcado como 'Vou Realizar'!", progress });
        }

        // PUT: api/progress/5/realizando - Marca "Realizando"
        [HttpPut("{wishId}/realizando")]
        public async Task<IActionResult> MarcarRealizando(int wishId, [FromBody] ProgressNotaDto dto)
        {
            var progress = await _context.WishProgresses
                .FirstOrDefaultAsync(p => p.WishId == wishId);

            if (progress == null)
            {
                return NotFound(new { message = "Wish nao encontrado" });
            }

            progress.StatusSecreto = StatusSecreto.Realizando.ToString();
            progress.NotaPrivada = dto.NotaPrivada;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Status atualizado para 'Realizando'!", progress });
        }

        // PUT: api/progress/5/realizar - Marca como Realizado (ela ve!)
        [HttpPut("{wishId}/realizar")]
        public async Task<IActionResult> RealizarWish(int wishId, [FromBody] ProgressRealizadoDto dto)
        {
            var progress = await _context.WishProgresses
                .Include(p => p.Wish)
                .FirstOrDefaultAsync(p => p.WishId == wishId);

            if (progress == null)
            {
                return NotFound(new { message = "Wish nao encontrado" });
            }

            // Atualiza o Progress
            progress.StatusSecreto = StatusSecreto.Realizado.ToString();
            progress.DataRealizacao = DateTime.Now;
            progress.NotaRealizacao = dto.NotaRealizacao;
            progress.FotosRealizacao = dto.FotosRealizacao; // URLs separadas por virgula

            // Atualiza o Wish - AGORA ELA VE!
            progress.Wish.Status = WishStatus.Realizado.ToString();

            await _context.SaveChangesAsync();

            return Ok(new { message = "Desejo realizado! Ela pode ver agora", progress });
        }
    }

    // DTOs
    public class ProgressNotaDto
    {
        public string? NotaPrivada { get; set; }
    }

    public class ProgressRealizadoDto
    {
        public string? NotaRealizacao { get; set; }
        public string? FotosRealizacao { get; set; } // URLs separadas por virgula
    }
}

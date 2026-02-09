using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WishListAPI.Data;
using WishListAPI.DTOs;
using WishListAPI.Models;

namespace WishListAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WishController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WishController(AppDbContext context)
        {
            _context = context;
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return null;
            }

            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        private async Task<List<int>> GetNamoradaIdsByCodigoAsync(string codigoNamorada)
        {
            return await _context.Users
                .Where(u => u.Role == UserRole.Namorada.ToString() && u.CodigoNamorada == codigoNamorada)
                .Select(u => u.Id)
                .ToListAsync();
        }

        // GET: api/Wish
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Wish>>> GetWishes()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            var wishesQuery = _context.Wishes
                .Include(w => w.Progress)
                .AsQueryable();

            if (string.Equals(user.Role, UserRole.Namorado.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(user.CodigoNamorada))
                {
                    return Ok(new List<Wish>());
                }

                var namoradaIds = await GetNamoradaIdsByCodigoAsync(user.CodigoNamorada);
                if (namoradaIds.Count == 0)
                {
                    return Ok(new List<Wish>());
                }

                wishesQuery = wishesQuery.Where(w => namoradaIds.Contains(w.UsuarioId));
            }
            else
            {
                wishesQuery = wishesQuery.Where(w => w.UsuarioId == user.Id);
            }

            return await wishesQuery.ToListAsync();
        }

        // GET: api/Wish/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Wish>> GetWish(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            var wishesQuery = _context.Wishes
                .Include(w => w.Progress)
                .AsQueryable();

            if (string.Equals(user.Role, UserRole.Namorado.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(user.CodigoNamorada))
                {
                    return NotFound();
                }

                var namoradaIds = await GetNamoradaIdsByCodigoAsync(user.CodigoNamorada);
                if (namoradaIds.Count == 0)
                {
                    return NotFound();
                }

                wishesQuery = wishesQuery.Where(w => namoradaIds.Contains(w.UsuarioId));
            }
            else
            {
                wishesQuery = wishesQuery.Where(w => w.UsuarioId == user.Id);
            }

            var wish = await wishesQuery.FirstOrDefaultAsync(w => w.Id == id);

            if (wish == null)
            {
                return NotFound();
            }

            return wish;
        }

        // PUT: api/Wish/5/status
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Namorado")]
        public async Task<IActionResult> UpdateWishStatus(int id, [FromBody] WishStatusUpdateDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            if (!string.Equals(user.Role, UserRole.Namorado.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(user.CodigoNamorada))
            {
                return BadRequest(new { message = "Codigo da namorada nao informado." });
            }

            var namoradaIds = await GetNamoradaIdsByCodigoAsync(user.CodigoNamorada);
            if (namoradaIds.Count == 0)
            {
                return NotFound(new { message = "Nenhuma namorada vinculada a este codigo." });
            }

            var wish = await _context.Wishes.FirstOrDefaultAsync(w => w.Id == id && namoradaIds.Contains(w.UsuarioId));
            if (wish == null)
            {
                return NotFound(new { message = "Wish nao encontrado." });
            }

            wish.Status = dto.Status.ToString();

            var progress = await _context.WishProgresses
                .FirstOrDefaultAsync(p => p.WishId == wish.Id && p.NamoradoId == user.Id);

            if (progress == null)
            {
                progress = new WishProgress
                {
                    WishId = wish.Id,
                    NamoradoId = user.Id
                };
                _context.WishProgresses.Add(progress);
            }

            if (dto.Status == WishStatus.Realizado)
            {
                progress.StatusSecreto = StatusSecreto.Realizado.ToString();
                progress.DataRealizacao = DateTime.Now;
            }
            else
            {
                progress.StatusSecreto = StatusSecreto.Nao_Iniciado.ToString();
                progress.DataRealizacao = null;
                progress.NotaPrivada = null;
                progress.NotaRealizacao = null;
                progress.FotosRealizacao = null;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Status atualizado com sucesso!" });
        }

        // POST: api/Wish
        [HttpPost]
        public async Task<ActionResult<Wish>> PostWish(WishCreateDto wishDto)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            var wish = new Wish
            {
                UsuarioId = userId,
                Titulo = wishDto.Titulo,
                Descricao = wishDto.Descricao,
                Categoria = wishDto.Categoria.ToString(),
                Prioridade = wishDto.Prioridade.ToString(),
                ImagemUrl = wishDto.ImagemUrl,
                Link = wishDto.Link,
                Status = WishStatus.Pendente.ToString(),
                DataCriacao = DateTime.Now
            };

            _context.Wishes.Add(wish);
            await _context.SaveChangesAsync();

            var usuario = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (usuario != null && !string.IsNullOrWhiteSpace(usuario.CodigoNamorada))
            {
                var namorado = await _context.Users.FirstOrDefaultAsync(u =>
                    u.Role == UserRole.Namorado.ToString() && u.CodigoNamorada == usuario.CodigoNamorada);

                if (namorado != null)
                {
                    var progress = new WishProgress
                    {
                        WishId = wish.Id,
                        NamoradoId = namorado.Id,
                        StatusSecreto = StatusSecreto.Nao_Iniciado.ToString()
                    };

                    _context.WishProgresses.Add(progress);
                    await _context.SaveChangesAsync();
                }
            }

            return CreatedAtAction(nameof(GetWish), new { id = wish.Id }, wish);
        }

        // PUT: api/Wish/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutWish(int id, WishUpdateDto wishDto)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            var wish = await _context.Wishes.FirstOrDefaultAsync(w => w.Id == id && w.UsuarioId == userId);

            if (wish == null)
            {
                return NotFound();
            }

            if (wishDto.Titulo != null) wish.Titulo = wishDto.Titulo;
            if (wishDto.Descricao != null) wish.Descricao = wishDto.Descricao;
            if (wishDto.Categoria.HasValue) wish.Categoria = wishDto.Categoria.Value.ToString();
            if (wishDto.Prioridade.HasValue) wish.Prioridade = wishDto.Prioridade.Value.ToString();
            if (wishDto.ImagemUrl != null) wish.ImagemUrl = wishDto.ImagemUrl;
            if (wishDto.Link != null) wish.Link = wishDto.Link;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WishExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Wish/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWish(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            Wish? wish;

            if (string.Equals(user.Role, UserRole.Namorado.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(user.CodigoNamorada))
                {
                    return BadRequest(new { message = "Codigo da namorada nao informado." });
                }

                var namoradaIds = await GetNamoradaIdsByCodigoAsync(user.CodigoNamorada);
                if (namoradaIds.Count == 0)
                {
                    return NotFound();
                }

                wish = await _context.Wishes.FirstOrDefaultAsync(w => w.Id == id && namoradaIds.Contains(w.UsuarioId));
            }
            else
            {
                wish = await _context.Wishes.FirstOrDefaultAsync(w => w.Id == id && w.UsuarioId == user.Id);
            }

            if (wish == null)
            {
                return NotFound();
            }

            _context.Wishes.Remove(wish);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool WishExists(int id)
        {
            return _context.Wishes.Any(e => e.Id == id);
        }
    }

    public class WishStatusUpdateDto
    {
        public WishStatus Status { get; set; }
    }
}

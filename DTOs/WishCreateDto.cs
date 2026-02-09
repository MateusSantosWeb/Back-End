using WishListAPI.Models;

namespace WishListAPI.DTOs
{
    public class WishCreateDto
    {
        public string Titulo { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public WishCategoria Categoria { get; set; }
        public WishPrioridade Prioridade { get; set; } = WishPrioridade.Media;
        public string? ImagemUrl { get; set; }
        public string? Link { get; set; }
    }

    public class WishUpdateDto
    {
        public string? Titulo { get; set; }
        public string? Descricao { get; set; }
        public WishCategoria? Categoria { get; set; }
        public WishPrioridade? Prioridade { get; set; }
        public string? ImagemUrl { get; set; }
        public string? Link { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WishListAPI.Models
{
    public class Wish
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descricao { get; set; }

        [Required]
        public string Categoria { get; set; } = string.Empty;

        [Required]
        public string Prioridade { get; set; } = string.Empty;

        public string? Link { get; set; }

        public string? ImagemUrl { get; set; }

        public string Status { get; set; } = "Ativo";

        public DateTime DataCriacao { get; set; } = DateTime.Now;

        public int UsuarioId { get; set; }

        // Relacionamento com User
        [JsonIgnore]
        public User? Usuario { get; set; }

        // Relacionamento com WishProgress - IGNORADO para evitar ciclo
        [JsonIgnore]
        public WishProgress? Progress { get; set; }
    }
}
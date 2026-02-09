using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WishListAPI.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string SenhaHash { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Namorada"; // Namorada ou Namorado

        // Código único da namorada que o namorado usa para se cadastrar
        public string? CodigoNamorada { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.Now;

        // Relacionamentos - IGNORADOS para evitar ciclo
        [JsonIgnore]
        public ICollection<Wish>? Wishes { get; set; }

        [JsonIgnore]
        public ICollection<WishProgress>? Progressos { get; set; }
    }
}
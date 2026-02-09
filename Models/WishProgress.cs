using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WishListAPI.Models
{
    public class WishProgress
    {
        public int Id { get; set; }

        public int WishId { get; set; }

        public int NamoradoId { get; set; }

        // Status secreto que só o namorado vê
        public string StatusSecreto { get; set; } = "Nao_Iniciado"; // Nao_Iniciado, Vou_Realizar, Realizando, Realizado

        // Notas privadas do namorado
        public string? NotaPrivada { get; set; }

        // Quando ele realizar, escreve aqui
        public string? NotaRealizacao { get; set; }

        // URLs de fotos da realização
        public string? FotosRealizacao { get; set; }

        public DateTime? DataRealizacao { get; set; }

        // Relacionamentos - IGNORADOS para evitar ciclo
        [JsonIgnore]
        public Wish? Wish { get; set; }

        [JsonIgnore]
        public User? Namorado { get; set; }
    }
}
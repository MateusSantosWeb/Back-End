using System.ComponentModel.DataAnnotations;

namespace WishListAPI.DTOs;

public class RegisterDto
{
    [Required] [MaxLength(100)] public string Nome { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; }

    [Required]
    [MinLength(5)] public string Senha { get; set; }
    
    [Required]
    public string Role { get; set; }

    public string? CodigoNamorada { get; set; }
}    
    
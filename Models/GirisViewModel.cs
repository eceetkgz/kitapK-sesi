using System.ComponentModel.DataAnnotations;

public class GirisViewModel
{
    [Required(ErrorMessage = "E-posta alanı zorunludur")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    public string Eposta { get; set; }

    [Required(ErrorMessage = "Parola alanı zorunludur")]
    [MinLength(6, ErrorMessage = "Parola en az 6 karakter olmalıdır")]
    public string Parola { get; set; }
} 
using System.ComponentModel.DataAnnotations;

namespace KitapKosesi.Models
{
    public class GirisModel
    {
        [Required(ErrorMessage = "Email adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
        public string Eposta { get; set; }

        [Required(ErrorMessage = "Şifre gereklidir")]
        [DataType(DataType.Password)]
        public string Sifre { get; set; }
    }
} 
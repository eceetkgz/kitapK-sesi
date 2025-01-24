using KitapKosesi.Models;

namespace KitapKosesi.Models
{
    public class SepetViewModel
    {
        public Kitap Kitap { get; set; }
        public int Adet { get; set; }
        public decimal ToplamFiyat => Kitap.Fiyat * Adet;
    }
} 
using System;
using System.ComponentModel.DataAnnotations;

namespace KitapKosesi.Models
{
    public class SiparisModel
    {
        [Required(ErrorMessage = "Ad alanı zorunludur")]
        public string Ad { get; set; }

        [Required(ErrorMessage = "Soyad alanı zorunludur")]
        public string Soyad { get; set; }

        [Required(ErrorMessage = "E-posta alanı zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string EPosta { get; set; }

        [Required(ErrorMessage = "Telefon alanı zorunludur")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        public string TelefonNo { get; set; }

        [Required(ErrorMessage = "Adres alanı zorunludur")]
        public string Adres { get; set; }

        [Required(ErrorMessage = "İl seçimi zorunludur")]
        public string Il { get; set; }

        [Required(ErrorMessage = "İlçe seçimi zorunludur")]
        public string Ilce { get; set; }

        [Required(ErrorMessage = "Ödeme tipi seçimi zorunludur")]
        public string OdemeTipi { get; set; }

        // Kredi Kartı Bilgileri
        [Display(Name = "Kart Sahibi")]
        public string KartSahibiAdi { get; set; }

        [Display(Name = "Kart Numarası")]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "Geçerli bir kart numarası giriniz")]
        public string KartNumara { get; set; }

        [Display(Name = "Son Kullanma Tarihi")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/([0-9]{2})$", ErrorMessage = "Geçerli bir son kullanma tarihi giriniz (AA/YY)")]
        public string KartSonKullanma { get; set; }

        [Display(Name = "Güvenlik Kodu")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "Geçerli bir güvenlik kodu giriniz")]
        public string KartCVV { get; set; }

        [Required(ErrorMessage = "Sözleşmeyi kabul etmelisiniz")]
        public bool SozlesmeKabul { get; set; }

        public List<SepetUrunu> SepetUrunleri { get; set; }
        public decimal AraToplam { get; set; }
        public decimal KargoUcreti { get; set; }
        public decimal ToplamTutar { get; set; }

        public string SiparisNo { get; set; }
        public DateTime SiparisTarihi { get; set; }
        public string SiparisDurumu { get; set; }
        public string OdemeDurumu { get; set; }
        public string KargoTakipNo { get; set; }

        public List<SiparisDetayModel> SiparisDetaylari { get; set; }
        public string? Telefon { get; internal set; }

        public SiparisModel()
        {
            SepetUrunleri = new List<SepetUrunu>();
            SiparisDetaylari = new List<SiparisDetayModel>();
            SiparisDurumu = "Onay Bekliyor";
            SiparisTarihi = DateTime.Now;
        }
    }

    public class SiparisDetayModel
    {
        public string KitapId { get; set; }
        public string KitapAdi { get; set; }
        public int Adet { get; set; }
        public decimal BirimFiyat { get; set; }
        public decimal ToplamFiyat => Adet * BirimFiyat;
    }
} 
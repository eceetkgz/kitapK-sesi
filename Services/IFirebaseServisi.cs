using System.Threading.Tasks;
using System.Collections.Generic;
using KitapKosesi.Models;

namespace KitapKosesi.Services
{
    public interface IFirebaseServisi
    {
        Task<(bool basarili, string mesaj, string kullaniciKimligi)> GirisYap(string eposta, string sifre);
        Task<(bool basarili, string mesaj, string kullaniciKimligi)> KayitOl(string eposta, string sifre, string ad, string soyad);
        Task<(bool basarili, string mesaj, string kullaniciKimligi)> SifreSifirla(string eposta);

        // Kitap işlemleri
        Task<List<Kitap>> KitaplariGetir();
        Task<Kitap> GetById(string kitapId); // Yeni eklenen metot
        Task<List<Kitap>> FavorileriGetir(string kullaniciId);
        Task<bool> FavorilerdenKaldir(string kitapId, string kullaniciId);
        Task<bool> FavorilereEkle(string kullaniciId, string kitapId);
        Task<bool> SepeteEkle(string kullaniciId, string kitapId);
        // Sepet işlemleri
        Task<List<SepetUrunu>> SepetGetir(string kullaniciId);
        Task<bool> SepettenKaldir(string kitapId, string kullaniciId);
        Task<bool> SepetAdetGuncelle(string kitapId, string kullaniciId, int miktar);


        // Sipariş işlemleri
        Task<string> SiparisOlustur(SiparisModel model, string kullaniciId);
        Task<SiparisModel> SiparisGetir(string siparisId);
        Task<List<SiparisModel>> SiparisleriGetir(string kullaniciId);
        Task<List<SiparisModel>> TumSiparisleriGetir();
        Task<bool> SiparisDurumuGuncelle(string siparisId, string yeniDurum);
        Task<bool> SepetiTemizle(string kullaniciId);
        Task<bool> FavoriyeEkle(string kullaniciId, string kitapId);

    }
}

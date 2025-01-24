using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using KitapKosesi.Services;

namespace KitapKosesi.Controllers
{
    public class GirisController : Controller
    {
        private readonly IFirebaseServisi _firebaseServisi;

        public GirisController(IFirebaseServisi firebaseServisi)
        {
            _firebaseServisi = firebaseServisi;
        }

        [HttpPost]
        public async Task<IActionResult> GirisYap(string email, string sifre)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(sifre))
            {
                TempData["Hata"] = "Email ve şifre boş olamaz";
                return RedirectToAction("GirisYap", "Home");
            }

            try
            {
                var (basarili, mesaj, userId) = await _firebaseServisi.GirisYap(email, sifre);
                
                if (basarili)
                {
                    HttpContext.Session.SetString("UserId", userId);
                    HttpContext.Session.SetString("UserEmail", email);
                    HttpContext.Session.SetString("IsLoggedIn", "true");
                    return RedirectToAction("Index", "Kitap");
                }
                
                TempData["Hata"] = mesaj;
                return RedirectToAction("GirisYap", "Home");
            }
            catch (Exception ex)
            {
                TempData["Hata"] = "Giriş yapılırken bir hata oluştu: " + ex.Message;
                return RedirectToAction("GirisYap", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SifreSifirla(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["Hata"] = "Email adresi boş olamaz";
                return RedirectToAction("GirisYap", "Home");
            }

            try
            {
                var (basarili, mesaj, _) = await _firebaseServisi.SifreSifirla(email);
                
                if (basarili)
                {
                    TempData["Basarili"] = "Şifre sıfırlama bağlantısı email adresinize gönderildi";
                }
                else
                {
                    TempData["Hata"] = mesaj;
                }
            }
            catch (Exception ex)
            {
                TempData["Hata"] = "Şifre sıfırlama işlemi başarısız: " + ex.Message;
            }

            return RedirectToAction("GirisYap", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> KayitOl(string ad, string soyad, string email, string sifre)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(sifre) || 
                string.IsNullOrEmpty(ad) || string.IsNullOrEmpty(soyad))
            {
                TempData["Hata"] = "Tüm alanları doldurunuz";
                return RedirectToAction("KayitOl");
            }

            try
            {
                var (basarili, mesaj, _) = await _firebaseServisi.KayitOl(email, sifre, ad, soyad);
                
                if (basarili)
                {
                    TempData["Basarili"] = "Kayıt işlemi başarılı. Lütfen giriş yapın.";
                    return RedirectToAction("GirisYap", "Home");
                }
                
                TempData["Hata"] = mesaj;
                return RedirectToAction("KayitOl");
            }
            catch (Exception ex)
            {
                TempData["Hata"] = "Kayıt işlemi başarısız: " + ex.Message;
                return RedirectToAction("KayitOl");
            }
        }

        [HttpPost]
        public IActionResult CikisYap()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult KayitOl()
        {
            return View();
        }
    }
}
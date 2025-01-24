using Microsoft.AspNetCore.Mvc;
using KitapKosesi.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace KitapKosesi.Controllers
{
    public class KayitController : Controller
    {
        private readonly IFirebaseServisi _firebaseServisi;

        public KayitController(IFirebaseServisi firebaseServisi)
        {
            _firebaseServisi = firebaseServisi;
        }

        [HttpGet]
        public IActionResult KayitOl()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> KayitOl(string ad, string soyad, string email, string sifre)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(sifre) || 
                string.IsNullOrEmpty(ad) || string.IsNullOrEmpty(soyad))
            {
                TempData["Hata"] = "Tüm alanları doldurunuz";
                return View();
            }

            var (basarili, mesaj, _) = await _firebaseServisi.KayitOl(email, sifre, ad, soyad);
            
            if (basarili)
            {
                TempData["Basarili"] = "Kayıt işlemi başarılı. Lütfen giriş yapın.";
                return RedirectToAction("GirisYap", "Anasayfa");
            }
            
            TempData["Hata"] = mesaj;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GirisYap(string email, string sifre)
        {
            var (basarili, mesaj, kullaniciKimligi) = await _firebaseServisi.GirisYap(email, sifre);

            if (basarili)
            {
                HttpContext.Session.SetString("UserId", kullaniciKimligi);
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("IsLoggedIn", "true");
                return RedirectToAction("Index", "Kitap");
            }
            else
            {
                ViewBag.Hata = mesaj;
                return View();
            }
        }
    }
}
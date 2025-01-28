using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using KitapKosesi.Models;    // ErrorViewModel için
using KitapKosesi.Services;  // IFirebaseServisi için

namespace KitapKosesi.Controllers
{
    public class AnasayfaController : Controller
    {
        private readonly ILogger<AnasayfaController> _logger;
        private readonly IFirebaseServisi _firebaseServisi;

        public AnasayfaController(ILogger<AnasayfaController> logger, IFirebaseServisi firebaseServisi)
        {
            _logger = logger;
            _firebaseServisi = firebaseServisi;
        }

   
        public async Task<IActionResult> Index()
        {
            var kitaplar = await _firebaseServisi.KitaplariGetir(); // Firebase'den kitapları al
            return View(kitaplar); // Kitapları view'a gönder
        }

        // Kayıt sayfasına yönlendirme
        public IActionResult KayitOl()
        {
            return View();
        }

        // Giriş sayfasına yönlendirme
        public IActionResult GirisYap()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GirisYap(string email, string sifre)
        {
            try
            {
                var (basarili, mesaj, kullaniciKimligi) = await _firebaseServisi.GirisYap(email, sifre);
                if (basarili)
                {
                    // Kullanıcı bilgilerini session'a kaydet
                    HttpContext.Session.SetString("UserEmail", email);
                    HttpContext.Session.SetString("IsLoggedIn", "true");
                    
                    // Kitap/Index sayfasına yönlendir
                    return RedirectToAction("Index", "Kitap");
                }
                
                TempData["Hata"] = mesaj;
                return View();
            }
            catch (Exception ex)
            {
                TempData["Hata"] = "Giriş yapılırken bir hata oluştu: " + ex.Message;
                return View();
            }
        }

        public IActionResult CikisYap()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

       


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Hata()
        {
            return View(new HataViewModel 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
            });
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using KitapKosesi.Services;
using KitapKosesi.Models;
using Google.Cloud.Firestore;

namespace KitapKosesi.Controllers
{
    public class KitapController : Controller
    {
        private readonly IFirebaseServisi _firebaseServisi;
        private readonly string _projectId;

        public KitapController(IFirebaseServisi firebaseServisi)
        {
            _firebaseServisi = firebaseServisi;
            _projectId = "kitapkosesi-b363e"; 
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var kitaplar = await _firebaseServisi.KitaplariGetir();
                return View(kitaplar);
            }
            catch (Exception ex)
            {
                TempData["Hata"] = "Kitaplar getirilirken bir hata oluştu: " + ex.Message;
                return View(new List<Kitap>());
            }
        }

        [HttpPost]
        public async Task<JsonResult> FavoriyeEkle(string kitapId)
        {
            try
            {
               
                await Task.Delay(100); // Simüle edilmiş işlem
                return Json(new { basarili = true, mesaj = "Kitap favorilere eklendi." });
            }
            catch (Exception ex)
            {
                return Json(new { basarili = false, mesaj = "Favorilere eklenirken bir hata oluştu: " + ex.Message });
            }
        }

    }

}

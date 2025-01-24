using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using KitapKosesi.Services;

namespace KitapKosesi.Controllers
{
    public class FavoriController : Controller
    {
        private readonly IFirebaseServisi _firebaseServisi;

        public FavoriController(IFirebaseServisi firebaseServisi)
        {
            _firebaseServisi = firebaseServisi;
        }

        [HttpPost]
        public async Task<IActionResult> FavoriyeEkle(string kitapId)
        {
            var kullaniciId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(kullaniciId))
            {
                return Json(new { success = false, message = "Lütfen önce giriþ yapýn" });
            }

            try
            {
                var sonuc = await _firebaseServisi.FavoriyeEkle(kullaniciId, kitapId);
                return Json(new { success = true, message = sonuc ? "Ürün favorilere eklendi" : "Ürün favorilerden çýkarýldý" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }
    }
}
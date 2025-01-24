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
                return Json(new { success = false, message = "L�tfen �nce giri� yap�n" });
            }

            try
            {
                var sonuc = await _firebaseServisi.FavoriyeEkle(kullaniciId, kitapId);
                return Json(new { success = true, message = sonuc ? "�r�n favorilere eklendi" : "�r�n favorilerden ��kar�ld�" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }
    }
}
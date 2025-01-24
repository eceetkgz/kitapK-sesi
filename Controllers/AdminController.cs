using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using KitapKosesi.Services;

namespace KitapKosesi.Controllers
{
    public class YoneticiController : Controller
    {
        private readonly IFirebaseServisi _firebaseServisi;

        public YoneticiController(IFirebaseServisi firebaseServisi)
        {
            _firebaseServisi = firebaseServisi;
        }

        public async Task<IActionResult> Siparisler()
        {
            var siparisler = await _firebaseServisi.TumSiparisleriGetir();
            return View(siparisler);
        }

        [HttpPost]
        public async Task<IActionResult> SiparisDurumuGuncelle(string siparisKimligi, string yeniDurum)
        {
            var basarili = await _firebaseServisi.SiparisDurumuGuncelle(siparisKimligi, yeniDurum);
            return Json(new { basarili });
        }

        
    }
} 
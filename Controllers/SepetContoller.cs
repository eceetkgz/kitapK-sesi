using KitapKosesi.Services;
using KitapKosesi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; 

public class SepetController : Controller
{
    private readonly IFirebaseServisi _kitapService;
    private readonly ILogger<SepetController> _logger; 

    public SepetController(IFirebaseServisi kitapService, ILogger<SepetController> logger) 
    {
        _kitapService = kitapService;
        _logger = logger; // ILogger'ı atayın
    }

    [HttpPost]
    public async Task<IActionResult> SepeteEkle(string kitapId)
    {
        try
        {
            if (string.IsNullOrEmpty(kitapId))
            {
                return Json(new { success = false, message = "Kitap ID boş olamaz." });
            }

            var kullaniciId = HttpContext.Session.GetString("User Id");
            if (string.IsNullOrEmpty(kullaniciId))
            {
                return Json(new { success = false, message = "Lütfen önce giriş yapın" });
            }

            var sepet = HttpContext.Session.GetObject<List<SepetUrunu>>("Sepet") ?? new List<SepetUrunu>();

            // GetById metodunu await ile çağırın
            var kitap = await _kitapService.GetById(kitapId);

            if (kitap == null)
            {
                _logger.LogWarning($"Kitap bulunamadı. Kitap ID: {kitapId}"); 
                return Json(new { success = false, message = "Kitap bulunamadı" });
            }

            var sepetItem = sepet.FirstOrDefault(x => x.KitapId == kitapId);
            if (sepetItem == null)
            {
                sepet.Add(new SepetUrunu
                {
                    KitapId = kitapId,
                    Adet = 1,
                    fiyat = kitap.Fiyat, 
                });
            }
            else
            {
                sepetItem.Adet++;
            }

            HttpContext.Session.SetObject("Sepet", sepet);
            return Json(new { success = true, message = "Ürün sepete eklendi" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sepete eklenirken hata oluştu."); 
            return Json(new { success = false, message = "Hata: " + ex.Message });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using Firebase.Auth;
using FirebaseAdmin.Auth;
using KitapKosesi.Services;
using KitapKosesi.Models;

public class KullaniciController : Controller
{
    private readonly IFirebaseServisi _firebaseServisi;

    public KullaniciController(IFirebaseServisi firebaseServisi)
    {
        _firebaseServisi = firebaseServisi;
    }

    [HttpGet]
    public IActionResult GirisYap()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> GirisYap(GirisModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Mesaj"] = "Lütfen tüm alanları doldurun.";
            return View(model);
        }

        try
        {
            var (basarili, mesaj, kullaniciKimligi) = await _firebaseServisi.GirisYap(model.Eposta, model.Sifre);

            if (basarili)
            {
                HttpContext.Session.SetString("UserId", kullaniciKimligi);
                HttpContext.Session.SetString("UserEmail", model.Eposta);
                HttpContext.Session.SetString("IsLoggedIn", "true");

                return RedirectToAction("Index", "Anasayfa");
            }
            else
            {
                TempData["Mesaj"] = mesaj;
                return View(model);
            }
        }
        catch (Exception ex)
        {
            TempData["Mesaj"] = "Giriş yapılırken bir hata oluştu: " + ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult KayitOl()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> KayitOl(string email, string sifre, string ad, string soyad)
    {
        var (basarili, mesaj, _) = await _firebaseServisi.KayitOl(email, sifre, ad, soyad);
        
        if (basarili)
        {
            TempData["Mesaj"] = "Kayıt başarılı! Şimdi giriş yapabilirsiniz.";
            return RedirectToAction("GirisYap");
        }
        else
        {
            ViewBag.Hata = mesaj;
            return View();
        }
    }

    [HttpGet]
    public IActionResult GirisKontrol()
    {
        var kullaniciKimligi = HttpContext.Session.GetString("UserId");
        return Json(new { 
            girisYapildi = !string.IsNullOrEmpty(kullaniciKimligi),
            yonlendirmeUrl = "/Kullanici/GirisYap"
        });
    }

    [HttpPost]
    public async Task<IActionResult> SifreSifirla(string eposta)
    {
        try
        {
            var (basarili, mesaj, _) = await _firebaseServisi.SifreSifirla(eposta);

            if (basarili)
            {
                TempData["Mesaj"] = "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi.";
            }
            else
            {
                TempData["Mesaj"] = mesaj;
            }
        }
        catch (Exception ex)
        {
            TempData["Mesaj"] = "Şifre sıfırlama işlemi başarısız: " + ex.Message;
        }

        return RedirectToAction("GirisYap");
    }

    [HttpPost]
    public IActionResult CikisYap()
    {
        HttpContext.Session.Clear();
        return Json(new { basarili = true, mesaj = "Çıkış yapıldı" });

    }
}

using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using KitapKosesi.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Firebase.Database;
using Microsoft.AspNetCore.DataProtection;

namespace KitapKosesi.Services
{
    public class FirebaseServisi : IFirebaseServisi
    {
        private readonly IConfiguration _yapilandirma;
        private readonly FirestoreDb _firestoreDb;
        private readonly FirebaseAuth _auth;
        private readonly IEPostaServisi _emailServisi;

        public FirebaseServisi(IConfiguration yapilandirma, IEPostaServisi emailServisi)
        {
            _yapilandirma = yapilandirma;
            _emailServisi = emailServisi;

            // Firebase Admin SDK'yı başlat
            if (FirebaseApp.DefaultInstance == null)
            {
                var kimlikDosyaYolu = _yapilandirma.GetSection("Firebase:CredentialPath").Value;
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", kimlikDosyaYolu);

                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile(kimlikDosyaYolu),
                    ProjectId = _yapilandirma.GetSection("Firebase:ProjectId").Value
                });
            }

            _auth = FirebaseAuth.DefaultInstance;
            _firestoreDb = FirestoreDb.Create(_yapilandirma.GetSection("Firebase:ProjectId").Value);
        }

        public async Task<(bool basarili, string mesaj, string kullaniciKimligi)> GirisYap(string eposta, string sifre)
        {
            try
            {
                if (string.IsNullOrEmpty(eposta) || string.IsNullOrEmpty(sifre))
                {
                    return (false, "Eposta ve şifre boş olamaz", null);
                }

                var apiAnahtari = _yapilandirma.GetSection("Firebase:ApiKey").Value;
                var kimlikDogrulamaUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiAnahtari}";

                using (var istemci = new HttpClient())
                {
                    var icerik = new StringContent(JsonSerializer.Serialize(new
                    {
                        email = eposta,
                        password = sifre,
                        returnSecureToken = true
                    }), Encoding.UTF8, "application/json");

                    var yanit = await istemci.PostAsync(kimlikDogrulamaUrl, icerik);
                    var yanitIcerik = await yanit.Content.ReadAsStringAsync();

                    if (yanit.IsSuccessStatusCode)
                    {
                        var sonuc = JsonSerializer.Deserialize<JsonElement>(yanitIcerik);
                        var kullaniciKimligi = sonuc.GetProperty("localId").GetString();
                        return (true, "Giriş başarılı", kullaniciKimligi);
                    }
                    else
                    {
                        var hata = JsonSerializer.Deserialize<JsonElement>(yanitIcerik);
                        var hataMesaji = hata.GetProperty("error").GetProperty("message").GetString();
                        return (false, HataMesajiAl(hataMesaji), null);
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, "Beklenmeyen bir hata oluştu: " + ex.Message, null);
            }
        }

        private string HataMesajiAl(string hataKodu)
        {
            return hataKodu switch
            {
                "EMAIL_NOT_FOUND" => "Bu eposta adresi kayıtlı değil",
                "INVALID_PASSWORD" => "Şifre hatalı",
                "USER_DISABLED" => "Kullanıcı hesabı devre dışı bırakılmış",
                "INVALID_EMAIL" => "Geçersiz eposta adresi",
                _ => "Giriş yapılırken bir hata oluştu"
            };
        }

        public async Task<(bool basarili, string mesaj, string kullaniciKimligi)> KayitOl(string eposta, string sifre, string ad, string soyad)
        {
            try
            {
                var userArgs = new UserRecordArgs()
                {
                    Email = eposta,
                    Password = sifre,
                    DisplayName = $"{ad} {soyad}",
                    EmailVerified = false
                };

                var userRecord = await _auth.CreateUserAsync(userArgs);

                if (userRecord != null)
                {
                    var kullaniciRef = _firestoreDb.Collection("kullanicilar").Document(userRecord.Uid);
                    await kullaniciRef.SetAsync(new Dictionary<string, object>
                    {
                        { "Ad", ad },
                        { "Soyad", soyad },
                        { "Email", eposta },
                        { "DisplayName", $"{ad} {soyad}" },
                        { "KayitTarihi", DateTime.UtcNow }
                    });

                    return (basarili: true, mesaj: "Kayıt başarılı", kullaniciKimligi: userRecord.Uid);
                }
                return (basarili: false, mesaj: "Kayıt başarısız", kullaniciKimligi: null);
            }
            catch (FirebaseAuthException ex)
            {
                return (basarili: false, mesaj: "Kayıt başarısız: " + ex.Message, kullaniciKimligi: null);
            }
            catch (Exception ex)
            {
                return (basarili: false, mesaj: "Beklenmeyen bir hata oluştu: " + ex.Message, kullaniciKimligi: null);
            }
        }

        public async Task<(bool basarili, string mesaj, string kullaniciKimligi)> SifreSifirla(string eposta)
        {
            try
            {
                if (string.IsNullOrEmpty(eposta))
                {
                    return (false, "E-posta adresi boş olamaz", null);
                }

                var apiAnahtari = _yapilandirma.GetSection("Firebase:ApiKey").Value;
                var resetUrl = _yapilandirma.GetSection("Firebase:PasswordResetUrl").Value;

                var sifirlamaLinki = await _auth.GeneratePasswordResetLinkAsync(eposta, new ActionCodeSettings
                {
                    Url = resetUrl,
                    HandleCodeInApp = true
                });

                var epostaGonderildi = await _emailServisi.SifreSifirlamaEPostasiGonder(eposta, sifirlamaLinki);

                if (epostaGonderildi)
                {
                    return (true, "Şifre sıfırlama bağlantısı gönderildi", null);
                }
                else
                {
                    return (false, "Şifre sıfırlama bağlantısı gönderilemedi", null);
                }
            }
            catch (FirebaseAuthException ex)
            {
                return (false, "Şifre sıfırlama hatası: " + ex.Message, null);
            }
            catch (Exception ex)
            {
                return (false, "Beklenmeyen bir hata oluştu: " + ex.Message, null);
            }
        }


        public async Task<List<Kitap>> KitaplariGetir()
        {
            try
            {
                var kitaplarRef = _firestoreDb.Collection("kitaplar");
                var snapshot = await kitaplarRef.GetSnapshotAsync();

                if (snapshot.Documents.Count == 0)
                {
                    return new List<Kitap>(); // Eğer belge yoksa boş liste döndür
                }

                var kitaplar = new List<Kitap>();

                foreach (var doc in snapshot.Documents)
                {
                    try
                    {
                        var data = doc.ToDictionary();
                        var kitap = new Kitap
                        {
                            Id = doc.Id,
                            Ad = data["kitapAdi"]?.ToString(),
                            Yazar = data["yazar"]?.ToString(),
                            Kategori = data["kategori"]?.ToString(),
                            StokMiktari = data.ContainsKey("stokAdedi") ? Convert.ToInt32(data["stokAdedi"]) : 0,
                            Tur = data["tur"]?.ToString(),
                            YayinYili = data["yayinYili"]?.ToString(),
                            ResimUrl = data["resimUrl"]?.ToString(),
                            Fiyat = data.ContainsKey("fiyat") ? Convert.ToDecimal(data["fiyat"]) : 0m
                        };

                        if (!string.IsNullOrEmpty(kitap.Ad))
                        {
                            kitaplar.Add(kitap);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Kitap dönüştürme hatası: {ex.Message} - Doküman: {doc.Id}");
                    }
                }

                return kitaplar;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kitapları getirirken hata oluştu: {ex.Message}");
                throw; // Hatanın üst katmana iletilmesini sağla
            }
        }

        public async Task<bool> FavoriyeEkle(string kullaniciId, string kitapId)
        {
            try
            {
                var favoriRef = _firestoreDb.Collection("favoriler")
                    .Document(kullaniciId)
                    .Collection("kitaplar")
                    .Document(kitapId);

                var snapshot = await favoriRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    // Favori zaten varsa sil
                    await favoriRef.DeleteAsync();
                    return false;
                }
                else
                {
                    // Favori yoksa ekle
                    await favoriRef.SetAsync(new Dictionary<string, object>
                    {
                        { "kitapId", kitapId },
                        { "eklenmeTarihi", DateTime.UtcNow }
                    });
                    return true;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        

        public async Task<List<Kitap>> FavorileriGetir(string kullaniciId)
        {
            try
            {
                var favoriler = new List<Kitap>();
                var kullaniciRef = _firestoreDb.Collection("kullanicilar").Document(kullaniciId);
                var favorilerRef = kullaniciRef.Collection("favoriler");
                var snapshot = await favorilerRef.GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    var data = doc.ToDictionary();
                    var kitap = new Kitap
                    {
                        Id = doc.Id,
                        Ad = data["kitapAdi"].ToString(),
                        Yazar = data["yazar"].ToString(),
                        Fiyat = Convert.ToDecimal(data["fiyat"]),
                        ResimUrl = data["resimUrl"].ToString()
                    };
                    favoriler.Add(kitap);
                }

                return favoriler;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Favoriler getirilirken hata: {ex.Message}");
                return new List<Kitap>();
            }
        }

        public async Task<bool> FavorilerdenKaldir(string kitapId, string kullaniciId)
        {
            try
            {
                var favorilerRef = _firestoreDb.Collection("kullanicilar")
                    .Document(kullaniciId)
                    .Collection("favoriler")
                    .Document(kitapId);

                var doc = await favorilerRef.GetSnapshotAsync();
                if (!doc.Exists)
                {
                    return false;
                }

                await favorilerRef.DeleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Favorilerden kaldırırken hata: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SepeteEkle(string kullaniciId, string kitapId)
        {
            try
            {
                // Önce kullanıcı dokümanının varlığını kontrol et
                var kullaniciRef = _firestoreDb.Collection("kullanicilar").Document(kullaniciId);
                var kullaniciDoc = await kullaniciRef.GetSnapshotAsync();

                if (!kullaniciDoc.Exists)
                {
                    // Kullanıcı dokümanı yoksa oluştur
                    await kullaniciRef.SetAsync(new Dictionary<string, object>());
                }

                // Kitap bilgilerini al
                var kitapRef = _firestoreDb.Collection("kitaplar").Document(kitapId);
                var kitapDoc = await kitapRef.GetSnapshotAsync();

                if (!kitapDoc.Exists)
                {
                    Console.WriteLine("Kitap bulunamadı");
                    return false;
                }

                var kitapData = kitapDoc.ToDictionary();

                // Stok kontrolü
                int stokAdedi = Convert.ToInt32(kitapData["stokAdedi"]);
                if (stokAdedi <= 0)
                {
                    Console.WriteLine("Ürün stokta yok");
                    return false;
                }

                // Kullanıcının sepet koleksiyonuna ekle
                var sepetRef = kullaniciRef.Collection("sepet").Document(kitapId);

                // Mevcut sepet öğesini kontrol et
                var sepetDoc = await sepetRef.GetSnapshotAsync();
                int mevcutAdet = 0;

                if (sepetDoc.Exists)
                {
                    var sepetData = sepetDoc.ToDictionary();
                    mevcutAdet = Convert.ToInt32(sepetData["adet"]);
                }

                await sepetRef.SetAsync(new Dictionary<string, object>
                {
                    { "kitapId", kitapId },
                    { "kitapAdi", kitapData["kitapAdi"] },
                    { "yazar", kitapData["yazar"] },
                    { "fiyat", kitapData["fiyat"] },
                    { "resimUrl", kitapData["resimUrl"] },
                    { "kategori", kitapData["kategori"] },
                    { "tur", kitapData["tur"] },
                    { "yayinYili", kitapData["yayinYili"] },
                    { "adet", mevcutAdet + 1 },
                    { "guncellenmeTarihi", FieldValue.ServerTimestamp }
                });

                Console.WriteLine($"Kitap sepete eklendi. KullaniciId: {kullaniciId}, KitapId: {kitapId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sepete eklenirken hata: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<List<SepetUrunu>> SepetGetir(string kullaniciId)
        {
            try
            {
                var sepetItems = new List<SepetUrunu>();
                var kullaniciRef = _firestoreDb.Collection("kullanicilar").Document(kullaniciId);
                var sepetRef = kullaniciRef.Collection("sepet");
                var snapshot = await sepetRef.GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    try
                    {
                        var data = doc.ToDictionary();
                        var kitap = new Kitap
                        {
                            Id = doc.Id,
                            Ad = data["kitapAdi"].ToString(),
                            Yazar = data["yazar"].ToString(),
                            Kategori = data["kategori"].ToString(),
                            Tur = data["tur"].ToString(),
                            YayinYili = data["yayinYili"].ToString(),
                            ResimUrl = data["resimUrl"].ToString(),
                            Fiyat = Convert.ToDecimal(data["fiyat"])
                        };

                        sepetItems.Add(new SepetUrunu
                        {
                            KitapId = doc.Id,
                            KullaniciId = kullaniciId,
                            Adet = Convert.ToInt32(data["adet"]),
                            Kitap = kitap
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Sepet öğesi dönüştürme hatası: {ex.Message}");
                    }
                }

                return sepetItems;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sepet getirilirken hata: {ex.Message}");
                return new List<SepetUrunu>();
            }
        }

        public async Task<bool> SepettenKaldir(string kitapId, string kullaniciId)
        {
            try
            {
                var sepetRef = _firestoreDb.Collection("sepet")
                    .Document(kullaniciId)
                    .Collection("kitaplar")
                    .Document(kitapId);

                await sepetRef.DeleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sepetten kaldırırken hata oluştu: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SepetAdetGuncelle(string kitapId, string kullaniciId, int miktar)
        {
            try
            {
                var sepetRef = _firestoreDb.Collection("sepet")
                    .Document(kullaniciId)
                    .Collection("kitaplar")
                    .Document(kitapId);

                var doc = await sepetRef.GetSnapshotAsync();
                if (!doc.Exists) return false;

                var data = doc.ToDictionary();
                var mevcutAdet = Convert.ToInt32(data["adet"]);
                var yeniAdet = mevcutAdet + miktar;

                if (yeniAdet <= 0)
                {
                    await sepetRef.DeleteAsync();
                }
                else
                {
                    await sepetRef.UpdateAsync("adet", yeniAdet);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sepet adedi güncellenirken hata oluştu: {ex.Message}");
                return false;
            }
        }

        public async Task<string> SiparisOlustur(SiparisModel model, string kullaniciId)
        {
            try
            {
                Console.WriteLine("SiparisOlustur başladı");
                Console.WriteLine($"Kullanıcı ID: {kullaniciId}");
                Console.WriteLine($"Model bilgileri: Ad={model.Ad}, Email={model.EPosta}");

                var siparisNo = $"SP{DateTime.Now:yyyyMMddHHmmss}";
                Console.WriteLine($"Sipariş No oluşturuldu: {siparisNo}");

                var siparisRef = _firestoreDb.Collection("siparisler").Document();
                Console.WriteLine($"Sipariş referansı oluşturuldu: {siparisRef.Id}");

                var siparisData = new Dictionary<string, object>
                {
                    { "siparisNumarasi", siparisNo },
                    { "kullaniciId", kullaniciId },
                    { "kullaniciBilgileri", new Dictionary<string, object>
                        {
                            { "ad", model.Ad },
                            { "soyad", model.Soyad },
                            { "email", model.EPosta },
                            { "telefon", model.Telefon }
                        }
                    },
                    { "teslimatAdresi", new Dictionary<string, object>
                        {
                            { "adres", model.Adres },
                            { "il", model.Il },
                            { "ilce", model.Ilce }
                        }
                    },
                    { "urunler", model.SepetUrunleri.Select(item => new Dictionary<string, object>
                        {
                            { "kitapId", item.KitapKimligi },
                            { "kitapAdi", item.Kitap.Ad },
                            { "adet", item.Adet },
                            { "birimFiyat", Convert.ToDouble(item.Kitap.Fiyat) },
                            { "resimUrl", item.Kitap.ResimUrl ?? "" },
                            { "toplamFiyat", Convert.ToDouble(item.Kitap.Fiyat * item.Adet) }
                        }).ToList()
                    },
                    { "odemeBilgileri", new Dictionary<string, object>
                        {
                            { "odemeTipi", model.OdemeTipi },
                            { "kartSahibi", model.OdemeTipi == "KrediKarti" ? model.KartSahibiAdi : "" },
                            { "kartNumarasi", model.OdemeTipi == "KrediKarti" ? $"**** **** **** {model.KartNumara.Substring(model.KartNumara.Length - 4)}" : "" },
                            { "kartSonKullanmaTarihi", model.OdemeTipi == "KrediKarti" ? model.KartSonKullanma : "" },
                            { "kartGuvenlikKodu", model.OdemeTipi == "KrediKarti" ? model.KartCVV : "" },
                            { "odemeDurumu", "Beklemede" }
                        }
                    },
                    { "siparisDurumu", "Onay Bekliyor" },
                    { "siparisTarihi", DateTime.UtcNow },
                    { "araToplam", Convert.ToDouble(model.AraToplam) },
                    { "kargoUcreti", Convert.ToDouble(model.KargoUcreti) },
                    { "toplamTutar", Convert.ToDouble(model.ToplamTutar) }
                };

                Console.WriteLine("Sipariş verisi hazırlandı");

                await siparisRef.SetAsync(siparisData);
                Console.WriteLine("Sipariş Firestore'a kaydedildi");

                // Kullanıcı güncellemesi
                var kullaniciRef = _firestoreDb.Collection("kullanicilar").Document(kullaniciId);
                await kullaniciRef.UpdateAsync(new Dictionary<string, object>
                {
                    { $"siparisler.{siparisRef.Id}", new Dictionary<string, object>
                        {
                            { "siparisNumarasi", siparisNo },
                            { "siparisTarihi", DateTime.UtcNow },
                            { "toplamTutar", Convert.ToDouble(model.ToplamTutar) },
                            { "siparisDurumu", "Onay Bekliyor" }
                        }
                    }
                });

                Console.WriteLine("Kullanıcı bilgileri güncellendi");
                return siparisNo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SiparisOlustur hatası: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<bool> SepetiTemizle(string kullaniciId)
        {
            try
            {
                var sepetRef = _firestoreDb.Collection("sepet").Document(kullaniciId);
                var kitaplarRef = sepetRef.Collection("kitaplar");
                var snapshot = await kitaplarRef.GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    await doc.Reference.DeleteAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sepet temizlenirken hata: {ex.Message}");
                return false;
            }
        }

        public async Task<SiparisModel> SiparisGetir(string siparisId)
        {
            try
            {
                var siparisDoc = await _firestoreDb.Collection("siparisler").Document(siparisId).GetSnapshotAsync();
                if (!siparisDoc.Exists)
                {
                    return null;
                }

                var data = siparisDoc.ToDictionary();
                // Veriyi SiparisModel'e dönüştür
                // ... (detaylı dönüşüm kodu)
                return new SiparisModel(); // Dönüşüm kodunu ekleyin
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sipariş getirilirken hata: {ex.Message}");
                return null;
            }
        }

        public async Task<List<SiparisModel>> SiparisleriGetir(string kullaniciId)
        {
            try
            {
                var siparisler = new List<SiparisModel>();
                // Önce kullanıcıya ait siparişleri al
                var siparislerRef = _firestoreDb.Collection("siparisler")
                    .WhereEqualTo("kullaniciId", kullaniciId);

                var snapshot = await siparislerRef.GetSnapshotAsync();

                // Bellek üzerinde sıralama yap
                var siparisDocuments = snapshot.Documents
                    .OrderByDescending(doc => ((Timestamp)doc.GetValue<object>("siparisTarihi")).ToDateTime())
                    .ToList();

                foreach (var doc in siparisDocuments)
                {
                    var data = doc.ToDictionary();
                    var kullaniciBilgileri = data["kullaniciBilgileri"] as Dictionary<string, object>;
                    var teslimatAdresi = data["teslimatAdresi"] as Dictionary<string, object>;
                    var urunler = data["urunler"] as List<object>;

                    var siparis = new SiparisModel
                    {
                        SiparisNo = data["siparisNumarasi"].ToString(),
                        Ad = kullaniciBilgileri["ad"].ToString(),
                        Soyad = kullaniciBilgileri["soyad"].ToString(),
                        EPosta = kullaniciBilgileri["email"].ToString(),
                        Telefon = kullaniciBilgileri["telefon"].ToString(),
                        Adres = teslimatAdresi["adres"].ToString(),
                        Il = teslimatAdresi["il"].ToString(),
                        Ilce = teslimatAdresi["ilce"].ToString(),
                        SiparisTarihi = ((Timestamp)data["siparisTarihi"]).ToDateTime(),
                        SiparisDurumu = data["siparisDurumu"].ToString(),
                        AraToplam = Convert.ToDecimal(data["araToplam"]),
                        KargoUcreti = Convert.ToDecimal(data["kargoUcreti"]),
                        ToplamTutar = Convert.ToDecimal(data["toplamTutar"]),
                        SepetUrunleri = new List<SepetUrunu>()
                    };

                    foreach (Dictionary<string, object> urun in urunler)
                    {
                        siparis.SepetUrunleri.Add(new SepetUrunu
                        {
                            KitapKimligi = urun["kitapId"].ToString(),
                            Adet = Convert.ToInt32(urun["adet"]),
                            Kitap = new Kitap
                            {
                                Id = urun["kitapId"].ToString(),
                                Ad = urun["kitapAdi"].ToString(),
                                ResimUrl = urun["resimUrl"]?.ToString(),
                                Fiyat = Convert.ToDecimal(urun["birimFiyat"])
                            }
                        });
                    }

                    siparisler.Add(siparis);
                }

                return siparisler;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Siparişler getirilirken hata: {ex.Message}");
                return new List<SiparisModel>();
            }
        }

        public async Task<List<SiparisModel>> TumSiparisleriGetir()
        {
            try
            {
                var siparisler = new List<SiparisModel>();
                var siparislerRef = _firestoreDb.Collection("siparisler")
                    .OrderByDescending("siparisTarihi");

                var snapshot = await siparislerRef.GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    var data = doc.ToDictionary();
                    var kullaniciBilgileri = data["kullaniciBilgileri"] as Dictionary<string, object>;
                    var teslimatAdresi = data["teslimatAdresi"] as Dictionary<string, object>;
                    var urunler = data["urunler"] as List<object>;

                    var siparis = new SiparisModel
                    {
                        SiparisNo = data["siparisNumarasi"].ToString(),
                        Ad = kullaniciBilgileri["ad"].ToString(),
                        Soyad = kullaniciBilgileri["soyad"].ToString(),
                        EPosta = kullaniciBilgileri["email"].ToString(),
                        Telefon = kullaniciBilgileri["telefon"].ToString(),
                        Adres = teslimatAdresi["adres"].ToString(),
                        Il = teslimatAdresi["il"].ToString(),
                        Ilce = teslimatAdresi["ilce"].ToString(),
                        SiparisTarihi = ((Timestamp)data["siparisTarihi"]).ToDateTime(),
                        SiparisDurumu = data["siparisDurumu"].ToString(),
                        AraToplam = Convert.ToDecimal(data["araToplam"]),
                        KargoUcreti = Convert.ToDecimal(data["kargoUcreti"]),
                        ToplamTutar = Convert.ToDecimal(data["toplamTutar"]),
                        SepetUrunleri = new List<SepetUrunu>()
                    };

                    foreach (Dictionary<string, object> urun in urunler)
                    {
                        siparis.SepetUrunleri.Add(new SepetUrunu
                        {
                            KitapKimligi = urun["kitapId"].ToString(),
                            Adet = Convert.ToInt32(urun["adet"]),
                            Kitap = new Kitap
                            {
                                Id = urun["kitapId"].ToString(),
                                Ad = urun["kitapAdi"].ToString(),
                                ResimUrl = urun["resimUrl"]?.ToString(),
                                Fiyat = Convert.ToDecimal(urun["birimFiyat"])
                            }
                        });
                    }

                    siparisler.Add(siparis);
                }

                return siparisler;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tüm siparişler getirilirken hata: {ex.Message}");
                return new List<SiparisModel>();
            }
        }

        public async Task<bool> SiparisDurumuGuncelle(string siparisId, string yeniDurum)
        {
            try
            {
                var siparisRef = _firestoreDb.Collection("siparisler").Document(siparisId);
                var siparis = await siparisRef.GetSnapshotAsync();

                if (!siparis.Exists)
                {
                    return false;
                }

                await siparisRef.UpdateAsync("siparisDurumu", yeniDurum);

                // Kullanıcının siparişler koleksiyonunu da güncelle
                var data = siparis.ToDictionary();
                var kullaniciId = data["kullaniciId"].ToString();
                var kullaniciRef = _firestoreDb.Collection("kullanicilar").Document(kullaniciId);

                await kullaniciRef.UpdateAsync(new Dictionary<string, object>
                {
                    { $"siparisler.{siparisId}.siparisDurumu", yeniDurum }
                });

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sipariş durumu güncellenirken hata: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> FavorilereEkle(string kullaniciId, string kitapId)
        {
            try
            {
                // Kullanıcı dokümanını kontrol et veya oluştur
                var kullaniciRef = _firestoreDb.Collection("kullanicilar").Document(kullaniciId);
                var kullaniciDoc = await kullaniciRef.GetSnapshotAsync();

                if (!kullaniciDoc.Exists)
                {
                    await kullaniciRef.SetAsync(new Dictionary<string, object>());
                }

                // Kitap bilgilerini al
                var kitapRef = _firestoreDb.Collection("kitaplar").Document(kitapId);
                var kitapDoc = await kitapRef.GetSnapshotAsync();

                if (!kitapDoc.Exists)
                {
                    Console.WriteLine("Kitap bulunamadı");
                    return false;
                }

                var kitapData = kitapDoc.ToDictionary();

                // Favoriler koleksiyonuna ekle
                var favorilerRef = kullaniciRef.Collection("favoriler").Document(kitapId);
                var favoriDoc = await favorilerRef.GetSnapshotAsync();

                if (favoriDoc.Exists)
                {
                    Console.WriteLine("Bu kitap zaten favorilerde");
                    return true;
                }

                await favorilerRef.SetAsync(new Dictionary<string, object>
            {
                { "kitapId", kitapId },
                { "kitapAdi", kitapData["kitapAdi"] },
                { "yazar", kitapData["yazar"] },
                { "fiyat", kitapData["fiyat"] },
                { "resimUrl", kitapData["resimUrl"] },
                { "kategori", kitapData["kategori"] },
                { "tur", kitapData["tur"] },
                { "yayinYili", kitapData["yayinYili"] },
                { "stokAdedi", kitapData["stokAdedi"] },
                { "eklemeTarihi", FieldValue.ServerTimestamp }
            });

                Console.WriteLine($"Kitap favorilere eklendi. KullaniciId: {kullaniciId}, KitapId: {kitapId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Favorilere eklenirken hata: {ex.Message}");
                return false;
            }
        }

        public Task<Kitap> GetById(string kitapId)
        {
            throw new NotImplementedException();
        }
    }

    
}
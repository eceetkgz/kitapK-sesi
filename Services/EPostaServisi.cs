using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace KitapKosesi.Services
{
    public interface IEPostaServisi
    {
        Task EPostaGonderAsync(string alici, string konu, string icerik);
        Task SiparisOnayEPostasiGonder(string eposta, string siparisNo, decimal toplamTutar);
        Task<bool> SifreSifirlamaEPostasiGonder(string eposta, string sifirlamaLinki);
    }

    public class EPostaServisi : IEPostaServisi
    {
        private readonly IConfiguration _yapilandirma;
        private readonly string _gonderenEposta;
        private readonly string _gonderenAd;
        private readonly string _smtpSunucu;
        private readonly int _smtpPort;
        private readonly string _kullaniciAdi;
        private readonly string _sifre;

        public EPostaServisi(IConfiguration yapilandirma)
        {
            _yapilandirma = yapilandirma ?? throw new ArgumentNullException(nameof(yapilandirma));

            var epostaAyarlari = _yapilandirma.GetSection("EpostaAyarlari");
            if (epostaAyarlari == null)
                throw new InvalidOperationException("EpostaAyarlari yapılandırması bulunamadı");

            _gonderenEposta = epostaAyarlari["GonderenEposta"] ?? 
                throw new InvalidOperationException("GonderenEposta ayarı bulunamadı");
            _gonderenAd = epostaAyarlari["GonderenAd"] ?? "Kitap Köşesi";
            _smtpSunucu = epostaAyarlari["SmtpSunucu"] ?? 
                throw new InvalidOperationException("SmtpSunucu ayarı bulunamadı");
            _smtpPort = int.Parse(epostaAyarlari["SmtpPort"] ?? "587");
            _kullaniciAdi = epostaAyarlari["KullaniciAdi"] ?? 
                throw new InvalidOperationException("KullaniciAdi ayarı bulunamadı");
            _sifre = epostaAyarlari["Sifre"] ?? 
                throw new InvalidOperationException("Sifre ayarı bulunamadı");
        }

        public async Task EPostaGonderAsync(string alici, string konu, string icerik)
        {
            try
            {
                var mesaj = new MailMessage();
                mesaj.From = new MailAddress(_gonderenEposta, _gonderenAd);
                mesaj.To.Add(new MailAddress(alici));
                mesaj.Subject = konu;
                mesaj.Body = icerik;
                mesaj.IsBodyHtml = true;

                using (var smtp = new SmtpClient(_smtpSunucu, _smtpPort))
                {
                    smtp.EnableSsl = true;
                    smtp.Credentials = new NetworkCredential(_kullaniciAdi, _sifre);
                    await smtp.SendMailAsync(mesaj);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"E-posta gönderilirken hata oluştu: {ex.Message}");
                throw;
            }
        }

        public async Task SiparisOnayEPostasiGonder(string eposta, string siparisNo, decimal toplamTutar)
        {
            var konu = "Sipariş Onayı";
            var epostaIcerik = $@"
                <h2>Sipariş Onayı</h2>
                <p>Sayın Müşterimiz,</p>
                <p>Siparişiniz başarıyla oluşturulmuştur.</p>
                <p>Sipariş Numarası: {siparisNo}</p>
                <p>Toplam Tutar: {toplamTutar:C}</p>
                <br>
                <p>Bizi tercih ettiğiniz için teşekkür ederiz.</p>
                <p>Kitap Köşesi</p>";

            await EPostaGonderAsync(eposta, konu, epostaIcerik);
        }

        public async Task<bool> SifreSifirlamaEPostasiGonder(string eposta, string sifirlamaLinki)
        {
            try
            {
                var konu = "Şifre Sıfırlama";
                var epostaIcerik = $@"
                    <h2>Şifre Sıfırlama İsteği</h2>
                    <p>Merhaba,</p>
                    <p>Şifrenizi sıfırlamak için aşağıdaki bağlantıya tıklayın:</p>
                    <p><a href='{sifirlamaLinki}'>Şifremi Sıfırla</a></p>
                    <p>Bu bağlantı 1 saat süreyle geçerlidir.</p>
                    <br>
                    <p>Eğer bu isteği siz yapmadıysanız, bu e-postayı dikkate almayın.</p>
                    <p>Saygılarımızla,</p>
                    <p>Kitap Köşesi</p>";

                await EPostaGonderAsync(eposta, konu, epostaIcerik);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Şifre sıfırlama e-postası gönderilirken hata: {ex.Message}");
                return false;
            }
        }
    }
} 
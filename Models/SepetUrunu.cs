using System;
using Google.Cloud.Firestore;

namespace KitapKosesi.Models
{
    public class SepetUrunu
    {
        internal object fiyat;

        [FirestoreProperty("kitapId")]
        public string KitapKimligi { get; set; }

        [FirestoreProperty("kullaniciId")]
        public string KullaniciId { get; set; }

        [FirestoreProperty("adet")]
        public int Adet { get; set; }

        [FirestoreProperty("eklenmeTarihi")]
        public DateTime EklenmeTarihi { get; set; }

        public Kitap Kitap { get; set; }
        public string KitapId { get; internal set; }

        public SepetUrunu()
        {
            EklenmeTarihi = DateTime.Now;
            Adet = 1;
        }
    }
}
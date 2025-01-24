using System;
using Google.Cloud.Firestore;

namespace KitapKosesi.Models
{
    [FirestoreData]
    public class Kitap
    {
        [FirestoreDocumentId]
        public string Id { get; set; }

        [FirestoreProperty("kitapAdi")]
        public string Ad { get; set; }

        [FirestoreProperty("yazar")]
        public string Yazar { get; set; }

        [FirestoreProperty("kategori")]
        public string Kategori { get; set; }

        [FirestoreProperty("stokAdedi")]
        public int StokMiktari { get; set; }

        [FirestoreProperty("tur")]
        public string Tur { get; set; }

        [FirestoreProperty("yayinYili")]
        public string YayinYili { get; set; }

        [FirestoreProperty("resimUrl")]
        public string ResimUrl { get; set; }

        [FirestoreProperty("fiyat")]
        public decimal Fiyat { get; set; }

        [FirestoreProperty("aciklama")]
        public string Aciklama { get; set; }

        public DateTime EklenmeTarihi { get; set; }
    }

    public class FavoriKitaplar
    {
        public string KitapId { get; set; }
        public string KullaniciId { get; set; }
        public DateTime EklenmeTarihi { get; set; }
    }
        
    public class StokBildirimi
    {
        public string KitapId { get; set; }
        public string KullaniciId { get; set; }
        public string KullaniciEmail { get; set; }
        public bool BildirimGonderildi { get; set; }
        public DateTime EklenmeTarihi { get; set; }
    }
}
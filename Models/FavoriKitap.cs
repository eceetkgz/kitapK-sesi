using Firebase.Auth;
using Firebase.Database;
using Firebase.Database.Query;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using KitapKosesi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Auth.Providers;

namespace KitapKosesi.Models
{
    public class favoriler
    {
        public string kullaniciId { get; set; }
        public string kitapId { get; set; }
    }

    public class sepet
    {
        public string kullaniciId { get; set; }
        public string kitapId { get; set; }
        public int Miktar { get; set; } = 1; 
    }


}

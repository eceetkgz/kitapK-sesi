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

    }
}
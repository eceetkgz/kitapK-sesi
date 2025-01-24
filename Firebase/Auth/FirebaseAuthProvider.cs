
namespace Firebase.Auth
{
    internal class FirebaseAuthProvider
    {
        private FirebaseConfig firebaseConfig;

        public FirebaseAuthProvider(FirebaseConfig firebaseConfig)
        {
            this.firebaseConfig = firebaseConfig;
        }

        internal async Task giris(string email, string sifre)
        {
            throw new NotImplementedException();
        }

        internal async Task SignInWithEmailAndPasswordAsync(string email, string sifre)
        {
            throw new NotImplementedException();
        }
    }
}
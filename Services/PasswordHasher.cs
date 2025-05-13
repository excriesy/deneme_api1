using System.Security.Cryptography;
using System.Text;

namespace ShareVault.API.Services
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
    }

    public class PasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            // Basit bir şifreleme örneği olarak SHA256 kullanıyorum
            // Gerçek uygulamalarda daha güvenli bir yöntem kullanabilirsiniz (PBKDF2, Bcrypt vb.)
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var hashedInputPassword = Convert.ToBase64String(hashedBytes);
                return hashedInputPassword == hashedPassword;
            }
        }
    }
}
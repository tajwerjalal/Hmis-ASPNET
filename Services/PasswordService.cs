using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace HmisWeb.Services
{
    public interface IPasswordService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
    }

    public class PasswordService : IPasswordService
    {
        public string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            string hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 32));
            return $"HMISPBKDF2$10000${Convert.ToBase64String(salt)}${hash}";
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return false;

            if (storedHash.StartsWith("HMISPBKDF2$"))
            {
                try
                {
                    var parts = storedHash.Split('$');
                    int iterations = int.Parse(parts[1]);
                    byte[] salt = Convert.FromBase64String(parts[2]);
                    byte[] expected = Convert.FromBase64String(parts[3]);

                    string actualHash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                        password: password,
                        salt: salt,
                        prf: KeyDerivationPrf.HMACSHA256,
                        iterationCount: iterations,
                        numBytesRequested: expected.Length));

                    return CryptographicOperations.FixedTimeEquals(
                        Convert.FromBase64String(actualHash), expected);
                }
                catch
                {
                    // Legacy or malformed hash - try plain compare below
                }
            }

            // Legacy plain-text fallback (pre-migration data)
            return string.Equals(password, storedHash, StringComparison.Ordinal);
        }
    }
}

using BCrypt.Net;

namespace Server.Common.Helpers
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password) =>
            BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10);

        public static bool VerifyPassword(string password, string passwordHash) =>
            BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}

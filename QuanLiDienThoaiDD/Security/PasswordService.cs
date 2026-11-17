using System;
using System.Web.Helpers;

namespace QuanLiDienThoaiDD.Security
{
    public static class PasswordService
    {
        public static string Hash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Mật khẩu không được để trống.", nameof(password));
            }

            return Crypto.HashPassword(password);
        }

        public static bool Verify(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
            {
                return false;
            }

            try
            {
                return Crypto.VerifyHashedPassword(passwordHash, password);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}

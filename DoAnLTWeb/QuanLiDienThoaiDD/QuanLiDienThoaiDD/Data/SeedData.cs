using System;
using System.Diagnostics;
using System.Linq;
using QuanLiDienThoaiDD.Models;
using QuanLiDienThoaiDD.Security;

namespace QuanLiDienThoaiDD.Data
{
    public static class SeedData
    {
        // Chỉ dùng cho dữ liệu mẫu của đồ án. Khi triển khai thật, nên đổi ngay các mật khẩu này.
        private static readonly DemoAccount[] DemoAccounts =
        {
            new DemoAccount("admin", "ADMIN", "admin@mobilehub.local", "Admin@123"),
            new DemoAccount("nhanvien", "NHANVIEN", "nhanvien@mobilehub.local", "Nhanvien@123"),
            new DemoAccount("khachhang", "KHACHHANG", "khachhang@mobilehub.local", "Khachhang@123")
        };

        public static void EnsureDemoPasswords()
        {
            try
            {
                using (var db = new QLHANGHOA_PHUKIEN_WEBEntities())
                {
                    if (!db.Database.Exists())
                    {
                        return;
                    }

                    var changed = false;
                    foreach (var demoAccount in DemoAccounts)
                    {
                        var account = db.TAIKHOANs.FirstOrDefault(x => x.TenDangNhap == demoAccount.UserName);
                        if (account == null || !IsPlaceholder(account.MatKhauHash))
                        {
                            continue;
                        }

                        account.MatKhauHash = PasswordService.Hash(demoAccount.Password);
                        account.TrangThai = true;
                        changed = true;
                    }

                    if (changed)
                    {
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                // Không làm ứng dụng mất khả năng khởi động nếu SQL Server chưa sẵn sàng.
                Trace.TraceWarning("Không thể đồng bộ mật khẩu dữ liệu mẫu: " + ex.Message);
            }
        }

        private static bool IsPlaceholder(string hash)
        {
            return string.IsNullOrWhiteSpace(hash)
                || hash.StartsWith("REPLACE_WITH_PASSWORD_HASH", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class DemoAccount
        {
            public DemoAccount(string userName, string role, string email, string password)
            {
                UserName = userName;
                Role = role;
                Email = email;
                Password = password;
            }

            public string UserName { get; private set; }
            public string Role { get; private set; }
            public string Email { get; private set; }
            public string Password { get; private set; }
        }
    }
}

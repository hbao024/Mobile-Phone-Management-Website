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

                    EnsureDemoOrdersAndReceipts(db);
                }
            }
            catch (Exception ex)
            {
                // Không làm ứng dụng mất khả năng khởi động nếu SQL Server chưa sẵn sàng.
                Trace.TraceWarning("Không thể đồng bộ dữ liệu mẫu: " + ex.Message);
            }
        }

        private static void EnsureDemoOrdersAndReceipts(QLHANGHOA_PHUKIEN_WEBEntities db)
        {
            // Nếu chỉ mới có 1 đơn hàng mẫu DH0001 thì bổ sung thêm các đơn ở đủ các trạng thái để thuận tiện kiểm thử & demo
            if (db.DONHANGs.Count() <= 1 && !db.DONHANGs.Any(x => x.MaDonHang == "DH0002"))
            {
                var adminAcc = db.TAIKHOANs.FirstOrDefault(x => x.TenDangNhap == "admin");
                var adminId = adminAcc != null ? (int?)adminAcc.MaTaiKhoan : null;
                var today = DateTime.Today;

                // DH0002: Chờ xác nhận (để demo bấm nút Xác nhận & Trừ tồn kho)
                var dh2 = new DONHANG
                {
                    MaDonHang = "DH0002",
                    MaKH = "KH002",
                    MaNVXuLy = null,
                    NgayDat = today.AddHours(9),
                    NgayCapNhat = today.AddHours(9),
                    TrangThaiDonHang = "CHO_XAC_NHAN",
                    PhuongThucThanhToan = "COD",
                    TrangThaiThanhToan = "CHUA_THANH_TOAN",
                    TenNguoiNhan = "Nguyễn Văn An",
                    SoDienThoaiNhan = "0911223344",
                    DiaChiGiaoHang = "Quận 1, TP. Hồ Chí Minh",
                    TamTinh = 18280000m,
                    PhiVanChuyen = 30000m,
                    GiamGia = 0m,
                    TongTien = 18310000m,
                    GhiChu = "Giao giờ hành chính, gọi trước 15 phút"
                };
                db.DONHANGs.Add(dh2);
                db.CHITIETDONHANGs.Add(new CHITIETDONHANG
                {
                    MaDonHang = "DH0002",
                    MaBienSanPham = "SP002-256-BLK",
                    TenSanPham = "Samsung Galaxy S24 - 256GB - Đen",
                    DonGia = 17990000m,
                    SoLuong = 1,
                    GiamGia = 0m
                });
                db.CHITIETDONHANGs.Add(new CHITIETDONHANG
                {
                    MaDonHang = "DH0002",
                    MaBienSanPham = "SP005-09-BLK",
                    TenSanPham = "Cáp sạc nhanh Anker - Đen",
                    DonGia = 290000m,
                    SoLuong = 1,
                    GiamGia = 0m
                });
                db.LICHSUTRANGTHAIDONHANGs.Add(new LICHSUTRANGTHAIDONHANG
                {
                    MaDonHang = "DH0002",
                    TrangThaiCu = null,
                    TrangThaiMoi = "CHO_XAC_NHAN",
                    MaTaiKhoan = adminId,
                    ThoiGian = today.AddHours(9),
                    GhiChu = "Khách hàng đặt đơn mới"
                });

                // DH0003: Đang giao hàng
                var dh3 = new DONHANG
                {
                    MaDonHang = "DH0003",
                    MaKH = "KH003",
                    MaNVXuLy = "NV002",
                    NgayDat = today.AddDays(-1).AddHours(14),
                    NgayCapNhat = today.AddHours(8),
                    TrangThaiDonHang = "DANG_GIAO",
                    PhuongThucThanhToan = "CHUYEN_KHOAN",
                    TrangThaiThanhToan = "DA_THANH_TOAN",
                    TenNguoiNhan = "Trần Minh Anh",
                    SoDienThoaiNhan = "0909887766",
                    DiaChiGiaoHang = "Quận Hải Châu, Đà Nẵng",
                    TamTinh = 10990000m,
                    PhiVanChuyen = 0m,
                    GiamGia = 200000m,
                    TongTien = 10790000m,
                    GhiChu = "Đã chuyển khoản thanh toán trước"
                };
                db.DONHANGs.Add(dh3);
                db.CHITIETDONHANGs.Add(new CHITIETDONHANG
                {
                    MaDonHang = "DH0003",
                    MaBienSanPham = "SP004-256-GRN",
                    TenSanPham = "OPPO Reno 11 - 256GB - Xanh",
                    DonGia = 10990000m,
                    SoLuong = 1,
                    GiamGia = 0m
                });
                db.LICHSUTRANGTHAIDONHANGs.Add(new LICHSUTRANGTHAIDONHANG
                {
                    MaDonHang = "DH0003",
                    TrangThaiCu = "CHO_XAC_NHAN",
                    TrangThaiMoi = "DA_XAC_NHAN",
                    MaTaiKhoan = adminId,
                    ThoiGian = today.AddDays(-1).AddHours(15),
                    GhiChu = "Đã xác nhận và xuất kho"
                });
                db.LICHSUTRANGTHAIDONHANGs.Add(new LICHSUTRANGTHAIDONHANG
                {
                    MaDonHang = "DH0003",
                    TrangThaiCu = "DA_XAC_NHAN",
                    TrangThaiMoi = "DANG_GIAO",
                    MaTaiKhoan = adminId,
                    ThoiGian = today.AddHours(8),
                    GhiChu = "Đang giao hàng cho khách"
                });

                // DH0004: Đã giao thành công hôm nay (để Dashboard hiện doanh thu hôm nay)
                var dh4 = new DONHANG
                {
                    MaDonHang = "DH0004",
                    MaKH = "KH001",
                    MaNVXuLy = "NV001",
                    NgayDat = today.AddHours(7),
                    NgayCapNhat = today.AddHours(11),
                    TrangThaiDonHang = "DA_GIAO",
                    PhuongThucThanhToan = "COD",
                    TrangThaiThanhToan = "DA_THANH_TOAN",
                    TenNguoiNhan = "Hoàng Gia Bảo",
                    SoDienThoaiNhan = "0987654321",
                    DiaChiGiaoHang = "TP. Hồ Chí Minh",
                    TamTinh = 15980000m,
                    PhiVanChuyen = 0m,
                    GiamGia = 0m,
                    TongTien = 15980000m,
                    GhiChu = "Khách mua 2 máy Xiaomi Redmi Note 13"
                };
                db.DONHANGs.Add(dh4);
                db.CHITIETDONHANGs.Add(new CHITIETDONHANG
                {
                    MaDonHang = "DH0004",
                    MaBienSanPham = "SP003-128-BLU",
                    TenSanPham = "Xiaomi Redmi Note 13 - 128GB - Xanh",
                    DonGia = 7990000m,
                    SoLuong = 2,
                    GiamGia = 0m
                });
                db.LICHSUTRANGTHAIDONHANGs.Add(new LICHSUTRANGTHAIDONHANG
                {
                    MaDonHang = "DH0004",
                    TrangThaiCu = "DANG_GIAO",
                    TrangThaiMoi = "DA_GIAO",
                    MaTaiKhoan = adminId,
                    ThoiGian = today.AddHours(11),
                    GhiChu = "Đã giao thành công và thu COD"
                });

                // PN0002: Phiếu nhập chờ xử lý để demo bấm nút Xác nhận nhập kho & Cộng tồn kho
                if (!db.PHIEUNHAPs.Any(x => x.MaPhieuNhap == "PN0002"))
                {
                    db.PHIEUNHAPs.Add(new PHIEUNHAP
                    {
                        MaPhieuNhap = "PN0002",
                        MaNCC = "NCC01",
                        MaNV = "NV002",
                        NgayNhap = today.AddHours(8),
                        TrangThai = "CHO_XU_LY",
                        TongTien = 78000000m,
                        GhiChu = "Lô hàng iPhone 15 bổ sung chờ kiểm đếm nhập kho"
                    });
                    db.CHITIETPHIEUNHAPs.Add(new CHITIETPHIEUNHAP
                    {
                        MaPhieuNhap = "PN0002",
                        MaBienSanPham = "SP001-128-BLK",
                        DonGia = 18000000m,
                        SoLuong = 2
                    });
                    db.CHITIETPHIEUNHAPs.Add(new CHITIETPHIEUNHAP
                    {
                        MaPhieuNhap = "PN0002",
                        MaBienSanPham = "SP001-256-BLU",
                        DonGia = 21000000m,
                        SoLuong = 2
                    });
                }

                db.SaveChanges();
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

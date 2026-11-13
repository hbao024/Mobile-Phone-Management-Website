using System;
using System.Linq;
using System.Web.Mvc;
using QuanLiDienThoaiDD.Models;
using QuanLiDienThoaiDD.Security;

namespace QuanLiDienThoaiDD.Controllers
{
    public class KhachHangController : Controller
    {
        QLHANGHOA_PHUKIEN_WEBEntities data = new QLHANGHOA_PHUKIEN_WEBEntities();

        public ActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangKy(DangKyModel kh)
        {
            if (data.TAIKHOANs.Any(t => t.TenDangNhap == kh.TenDangNhap))
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại");
            }
            if (data.TAIKHOANs.Any(t => t.Email == kh.Email))
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng");
            }

            if (!ModelState.IsValid)
            {
                return View(kh);
            }

            TAIKHOAN tk = new TAIKHOAN();
            tk.TenDangNhap = kh.TenDangNhap;
            tk.Email = kh.Email;
            tk.MatKhauHash = PasswordService.Hash(kh.MatKhau);
            tk.MaVaiTro = "KHACHHANG";
            tk.TrangThai = true;
            tk.NgayTao = DateTime.Now;
            data.TAIKHOANs.Add(tk);
            data.SaveChanges();

            KHACHHANG k = new KHACHHANG();
            k.MaKH = "KH" + (data.KHACHHANGs.Count() + 1).ToString("D3");
            k.TenKH = kh.TenKH;
            k.Email = kh.Email;
            k.DienThoai = kh.DienThoai;
            k.DiaChi = kh.DiaChi;
            k.NgayThamGia = DateTime.Now;
            k.MaTaiKhoan = tk.MaTaiKhoan;
            k.TrangThai = true;
            data.KHACHHANGs.Add(k);
            data.SaveChanges();

            TempData["SuccessMessage"] = "Đăng ký thành công, mời bạn đăng nhập.";
            return RedirectToAction("Login", "Account");
        }
    }
}
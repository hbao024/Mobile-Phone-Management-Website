using System;
using System.Linq;
using System.Web.Mvc;
using QuanLiDienThoaiDD.Models;

namespace QuanLiDienThoaiDD.Controllers
{
    public class DatHangController : Controller
    {
        QLHANGHOA_PHUKIEN_WEBEntities data = new QLHANGHOA_PHUKIEN_WEBEntities();

        [Authorize]
        public ActionResult ThanhToan()
        {
            GioHangTam gh = (GioHangTam)Session["gh"];
            if (gh == null || gh.SoMatHang() == 0)
            {
                return RedirectToAction("XemGioHang", "GioHang");
            }
            return View(gh);
        }

        [HttpPost]
        [Authorize]
        public ActionResult DatHang(FormCollection f)
        {
            GioHangTam gh = (GioHangTam)Session["gh"];
            if (gh == null || gh.SoMatHang() == 0)
            {
                return RedirectToAction("XemGioHang", "GioHang");
            }

            string ten = f["txtTen"].Trim();
            string sdt = f["txtSDT"].Trim();
            string diaChi = f["txtDiaChi"].Trim();

            if (ten == "" || sdt == "" || diaChi == "")
            {
                ViewBag.Loi = "Vui lòng nhập đầy đủ thông tin.";
                return View("ThanhToan", gh);
            }
            if (sdt.Length != 10 || sdt[0] != '0' || !sdt.All(char.IsDigit))
            {
                ViewBag.Loi = "Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0.";
                return View("ThanhToan", gh);
            }

            string tenDangNhap = User.Identity.Name;
            KHACHHANG kh = data.KHACHHANGs.FirstOrDefault(k => k.TAIKHOAN.TenDangNhap == tenDangNhap
                                                            || k.TAIKHOAN.Email == tenDangNhap);
            if (kh == null)
            {
                return RedirectToAction("Login", "Account");
            }

            DONHANG dh = new DONHANG();
            dh.MaDonHang = "DH" + (data.DONHANGs.Count() + 1).ToString("D4");
            dh.MaKH = kh.MaKH;
            dh.NgayDat = DateTime.Now;
            dh.NgayCapNhat = DateTime.Now;
            dh.TrangThaiDonHang = "CHO_XAC_NHAN";     
            dh.PhuongThucThanhToan = "COD";
            dh.TrangThaiThanhToan = "CHUA_THANH_TOAN";  
            dh.TenNguoiNhan = ten;
            dh.SoDienThoaiNhan = sdt;
            dh.DiaChiGiaoHang = diaChi;
            dh.GhiChu = f["txtGhiChu"];
            dh.TamTinh = gh.TongThanhTien();
            dh.PhiVanChuyen = 0;
            dh.GiamGia = 0;
            dh.TongTien = gh.TongThanhTien();
            data.DONHANGs.Add(dh);

            foreach (CartItem item in gh.lst)
            {
                CHITIETDONHANG ct = new CHITIETDONHANG();
                ct.MaDonHang = dh.MaDonHang;
                ct.MaBienSanPham = item.MaBienSanPham;
                ct.TenSanPham = item.TenSanPham;
                ct.DonGia = item.DonGia;
                ct.SoLuong = item.SoLuong;
                ct.GiamGia = 0;
                data.CHITIETDONHANGs.Add(ct);
            }

            data.SaveChanges();

            gh.XoaTatCa();
            Session["gh"] = gh;
            return RedirectToAction("ThanhCong", new { ma = dh.MaDonHang });
        }

        public ActionResult ThanhCong(string ma)
        {
            ViewBag.MaDonHang = ma;
            return View();
        }
    }
}
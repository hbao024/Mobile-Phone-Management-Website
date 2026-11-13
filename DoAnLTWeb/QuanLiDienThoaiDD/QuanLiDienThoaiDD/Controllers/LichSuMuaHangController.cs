using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using QuanLiDienThoaiDD.Models;

namespace QuanLiDienThoaiDD.Controllers
{
    public class LichSuMuaHangController : Controller
    {
        QLHANGHOA_PHUKIEN_WEBEntities data = new QLHANGHOA_PHUKIEN_WEBEntities();

        [Authorize]
        public ActionResult LichSu()
        {
            string tenDangNhap = User.Identity.Name;
            KHACHHANG kh = data.KHACHHANGs.FirstOrDefault(k => k.TAIKHOAN.TenDangNhap == tenDangNhap
                                                            || k.TAIKHOAN.Email == tenDangNhap);
            if (kh == null)
            {
                return RedirectToAction("Login", "Account");
            }

            List<DONHANG> ds = data.DONHANGs
                .Where(d => d.MaKH == kh.MaKH)
                .OrderByDescending(d => d.NgayDat)
                .ToList();

            return View(ds);
        }

        [Authorize]
        public ActionResult ChiTiet(string ma)
        {
            string tenDangNhap = User.Identity.Name;
            KHACHHANG kh = data.KHACHHANGs.FirstOrDefault(k => k.TAIKHOAN.TenDangNhap == tenDangNhap
                                                            || k.TAIKHOAN.Email == tenDangNhap);
            if (kh == null)
            {
                return RedirectToAction("Login", "Account");
            }

            DONHANG dh = data.DONHANGs.FirstOrDefault(d => d.MaDonHang == ma && d.MaKH == kh.MaKH);
            if (dh == null)
            {
                return HttpNotFound();
            }

            return View(dh);
        }
    }
}
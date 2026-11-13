using QuanLiDienThoaiDD.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QuanLiDienThoaiDD.Controllers
{
    public class SanPhamController : Controller
    {
        QLHANGHOA_PHUKIEN_WEBEntities data = new QLHANGHOA_PHUKIEN_WEBEntities();
        public ActionResult Index()
        {
            List<SANPHAM> ds = data.SANPHAMs
                .Where(s => s.TrangThai == true && s.BIENSANPHAMs.Count() > 0)
                .OrderBy(s => s.TenSanPham)
                .ToList();

            return View(ds);
        }

        public ActionResult XemChiTiet(string id)
        {
            SANPHAM sp = data.SANPHAMs.FirstOrDefault(s => s.MaSanPham == id);

            if (sp == null)
            {
                return HttpNotFound();
            }

            ViewBag.CungLoai = data.SANPHAMs
                .Where(s => s.MaLoai == sp.MaLoai && s.MaSanPham != sp.MaSanPham && s.TrangThai == true && s.BIENSANPHAMs.Count() > 0)
                .ToList();

            return View(sp);
        }

        public ActionResult TimKiem(string keyword)
        {
            if (keyword == null)
            {
                keyword = "";
            }

            List<SANPHAM> ds = data.SANPHAMs
                .Where(s => s.TenSanPham.Contains(keyword) && s.TrangThai == true && s.BIENSANPHAMs.Count() > 0)
                .OrderBy(s => s.TenSanPham)
                .ToList();

            ViewBag.Keyword = keyword;
            return View(ds);
        }

        public ActionResult LocSanPham(string maLoai, decimal? giaTu, decimal? giaDen)
        {
            var ds = data.SANPHAMs.Where(s => s.TrangThai == true && s.BIENSANPHAMs.Count() > 0);

            if (!string.IsNullOrEmpty(maLoai))
            {
                ds = ds.Where(s => s.MaLoai == maLoai);
            }

            if (giaTu != null)
            {
                ds = ds.Where(s => s.BIENSANPHAMs.Min(b => b.GiaBan) >= giaTu);
            }

            if (giaDen != null)
            {
                ds = ds.Where(s => s.BIENSANPHAMs.Min(b => b.GiaBan) <= giaDen);
            }

            ViewBag.LoaiSanPham = data.LOAISANPHAMs.ToList();
            ViewBag.MaLoai = maLoai;
            ViewBag.GiaTu = giaTu;
            ViewBag.GiaDen = giaDen;

            return View(ds.OrderBy(s => s.TenSanPham).ToList());
        }
    }
}
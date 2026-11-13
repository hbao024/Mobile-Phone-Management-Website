using QuanLiDienThoaiDD.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QuanLiDienThoaiDD.Controllers
{
    public class LoaiSanPhamController : Controller
    {
        QLHANGHOA_PHUKIEN_WEBEntities data = new QLHANGHOA_PHUKIEN_WEBEntities();
        public ActionResult DSMenu_LoaiSP()
        {
            List<LOAISANPHAM> ds = data.LOAISANPHAMs.ToList();
            return PartialView("LoaiSanPhamPartial", ds);
        }

        public ActionResult SanPhamTheoLoai(string maLoai)
        {
            List<SANPHAM> ds = data.SANPHAMs
                .Where(s => s.MaLoai == maLoai && s.TrangThai == true && s.BIENSANPHAMs.Count() > 0)
                .OrderBy(s => s.BIENSANPHAMs.Min(b => b.GiaBan))
                .ToList();

            return View(ds);
        }
    }
}
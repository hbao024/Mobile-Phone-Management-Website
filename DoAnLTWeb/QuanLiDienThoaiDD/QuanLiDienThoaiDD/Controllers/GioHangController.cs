using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QuanLiDienThoaiDD.Models;

namespace QuanLiDienThoaiDD.Controllers
{
    public class GioHangController : Controller
    {
        public ActionResult ThemMatHang(string maBienSanPham)
        {
            GioHangTam gh = (GioHangTam)Session["gh"];
            if (gh == null)
            {
                gh = new GioHangTam();
            }

            gh.Them(maBienSanPham);
            Session["gh"] = gh;

            return RedirectToAction("XemGioHang");
        }

        public ActionResult XemGioHang()
        {
            GioHangTam gh = (GioHangTam)Session["gh"];
            if (gh == null)
            {
                gh = new GioHangTam();
            }

            return View(gh);
        }

        public ActionResult _CartSummary()
        {
            GioHangTam gh = (GioHangTam)Session["gh"];
            int soLuong = 0;
            if (gh != null)
            {
                soLuong = gh.TongSLHang();
            }

            ViewBag.SoLuongGioHang = soLuong;
            return PartialView();
        }

        // xóa 1 sản phẩm khỏi giỏ
        public ActionResult XoaMatHang(string maBienSanPham)
        {
            GioHangTam gh = (GioHangTam)Session["gh"];
            if (gh != null)
            {
                gh.Xoa(maBienSanPham);
                Session["gh"] = gh;
            }

            return RedirectToAction("XemGioHang");
        }

        [HttpPost]
        public ActionResult CapNhat(string maBienSanPham, int soLuong)
        {
            GioHangTam gh = (GioHangTam)Session["gh"];
            if (gh != null)
            {
                gh.CapNhat(maBienSanPham, soLuong);
                Session["gh"] = gh;
            }

            return RedirectToAction("XemGioHang");
        }

        public ActionResult XoaGioHang()
        {
            GioHangTam gh = (GioHangTam)Session["gh"];
            if (gh != null)
            {
                gh.XoaTatCa();
                Session["gh"] = gh;
            }

            return RedirectToAction("XemGioHang");
        }
    }
}
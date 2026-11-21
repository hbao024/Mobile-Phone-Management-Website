using QuanLiDienThoaiDD.Models;
using QuanLiDienThoaiDD.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QuanLiDienThoaiDD.Controllers
{
    public class HomeController : Controller
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

        public ActionResult About()
        {
            ViewBag.Message = "Giới thiệu";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Liên hệ";
            return View(new ContactViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Contact(ContactViewModel model)
        {
            ViewBag.Message = "Liên hệ";
            if (!ModelState.IsValid)
                return View(model);

            TempData["ContactSuccess"] = "Cảm ơn bạn đã liên hệ. MobileHub sẽ phản hồi trong thời gian sớm nhất.";
            return RedirectToAction("Contact");
        }
    }
}

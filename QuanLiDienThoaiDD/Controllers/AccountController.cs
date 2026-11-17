using System;
using System.Linq;
using System.Web.Mvc;
using QuanLiDienThoaiDD.Models;
using QuanLiDienThoaiDD.Security;
using QuanLiDienThoaiDD.ViewModels;

namespace QuanLiDienThoaiDD.Controllers
{
    public class AccountController : Controller
    {
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (var db = new QLHANGHOA_PHUKIEN_WEBEntities())
            {
                var account = db.TAIKHOANs.FirstOrDefault(x =>
                    x.TenDangNhap == model.TenDangNhap || x.Email == model.TenDangNhap);

                if (account == null || !account.TrangThai
                    || !PasswordService.Verify(model.MatKhau, account.MatKhauHash))
                {
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập, mật khẩu không đúng hoặc tài khoản đã bị khóa.");
                    return View(model);
                }

                account.LanDangNhapCuoi = DateTime.Now;
                db.SaveChanges();

                AuthCookie.SignIn(account.TenDangNhap, account.MaVaiTro, model.GhiNhoDangNhap);

                if (account.MaVaiTro == "ADMIN" || account.MaVaiTro == "NHANVIEN")
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }

                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            AuthCookie.SignOut();
            TempData["SuccessMessage"] = "Bạn đã đăng xuất thành công.";
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public ActionResult AccessDenied()
        {
            Response.StatusCode = 403;
            return View();
        }
    }
}

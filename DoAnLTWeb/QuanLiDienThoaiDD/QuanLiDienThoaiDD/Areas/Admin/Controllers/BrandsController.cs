using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using QuanLiDienThoaiDD.Areas.Admin.ViewModels;
using QuanLiDienThoaiDD.Models;

namespace QuanLiDienThoaiDD.Areas.Admin.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class BrandsController : Controller
    {
        private readonly QLHANGHOA_PHUKIEN_WEBEntities db = new QLHANGHOA_PHUKIEN_WEBEntities();

        public ActionResult Index()
        {
            var brands = db.THUONGHIEUx.OrderBy(x => x.TenThuongHieu)
                .Select(x => new BrandViewModel
                {
                    MaThuongHieu = x.MaThuongHieu,
                    TenThuongHieu = x.TenThuongHieu,
                    MoTa = x.MoTa,
                    LogoUrl = x.LogoUrl,
                    TrangThai = x.TrangThai
                }).ToList();
            return View(brands);
        }

        public ActionResult Create()
        {
            return View(new BrandViewModel { TrangThai = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(BrandViewModel model)
        {
            Normalize(model);
            if (ModelState.IsValid)
            {
                if (db.THUONGHIEUx.Any(x => x.MaThuongHieu == model.MaThuongHieu))
                    ModelState.AddModelError("MaThuongHieu", "Mã thương hiệu đã tồn tại.");
                ValidateName(model.TenThuongHieu, null);
                if (ModelState.IsValid)
                {
                    db.THUONGHIEUx.Add(new THUONGHIEU
                    {
                        MaThuongHieu = model.MaThuongHieu,
                        TenThuongHieu = model.TenThuongHieu,
                        MoTa = model.MoTa,
                        LogoUrl = model.LogoUrl,
                        TrangThai = model.TrangThai
                    });
                    if (TrySave())
                    {
                        TempData["BrandSuccess"] = "Đã thêm thương hiệu.";
                        return RedirectToAction("Index");
                    }
                }
            }
            return View(model);
        }

        public ActionResult Edit(string id)
        {
            var brand = string.IsNullOrWhiteSpace(id) ? null : db.THUONGHIEUx.Find(id);
            if (brand == null) return HttpNotFound("Không tìm thấy thương hiệu.");

            ViewBag.BrandId = brand.MaThuongHieu;
            return View(new BrandEditViewModel
            {
                TenThuongHieu = brand.TenThuongHieu,
                MoTa = brand.MoTa,
                LogoUrl = brand.LogoUrl,
                TrangThai = brand.TrangThai
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string id, BrandEditViewModel model)
        {
            var brand = string.IsNullOrWhiteSpace(id) ? null : db.THUONGHIEUx.Find(id);
            if (brand == null) return HttpNotFound("Không tìm thấy thương hiệu.");

            ViewBag.BrandId = brand.MaThuongHieu;
            Normalize(model);
            if (ModelState.IsValid)
            {
                ValidateName(model.TenThuongHieu, brand.MaThuongHieu);
                if (ModelState.IsValid)
                {
                    brand.TenThuongHieu = model.TenThuongHieu;
                    brand.MoTa = model.MoTa;
                    brand.LogoUrl = model.LogoUrl;
                    brand.TrangThai = model.TrangThai;
                    if (TrySave())
                    {
                        TempData["BrandSuccess"] = "Đã cập nhật thương hiệu.";
                        return RedirectToAction("Index");
                    }
                }
            }
            return View(model);
        }

        private static void Normalize(BrandEditViewModel model)
        {
            model.TenThuongHieu = model.TenThuongHieu == null ? null : model.TenThuongHieu.Trim();
            model.MoTa = string.IsNullOrWhiteSpace(model.MoTa) ? null : model.MoTa.Trim();
            model.LogoUrl = string.IsNullOrWhiteSpace(model.LogoUrl) ? null : model.LogoUrl.Trim();
            var create = model as BrandViewModel;
            if (create != null) create.MaThuongHieu = create.MaThuongHieu == null ? null : create.MaThuongHieu.Trim();
        }

        private void ValidateName(string name, string excludedId)
        {
            // Use the database collation, matching its unique constraint (including inactive rows).
            if (db.THUONGHIEUx.Any(x => x.TenThuongHieu == name && (excludedId == null || x.MaThuongHieu != excludedId)))
                ModelState.AddModelError("TenThuongHieu", "Tên thương hiệu đã tồn tại, kể cả thương hiệu đã ngừng sử dụng.");
        }

        private bool TrySave()
        {
            try
            {
                db.SaveChanges();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("", "Dữ liệu đã thay đổi hoặc không còn tồn tại. Vui lòng tải lại danh sách.");
            }
            catch (DbUpdateException ex)
            {
                var sql = ex.GetBaseException() as SqlException;
                // The unique constraint remains authoritative if concurrent requests pass the pre-check.
                var duplicate = sql != null && (sql.Number == 2601 || sql.Number == 2627);
                ModelState.AddModelError("", duplicate
                    ? "Không thể lưu vì mã hoặc tên thương hiệu đã tồn tại. Vui lòng kiểm tra lại."
                    : "Không thể lưu thương hiệu. Vui lòng thử lại sau.");
            }
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}

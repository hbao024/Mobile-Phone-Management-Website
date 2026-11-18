using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using QuanLiDienThoaiDD.Areas.Admin.ViewModels;
using QuanLiDienThoaiDD.Models;

namespace QuanLiDienThoaiDD.Areas.Admin.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class CategoriesController : Controller
    {
        private readonly QLHANGHOA_PHUKIEN_WEBEntities db = new QLHANGHOA_PHUKIEN_WEBEntities();

        public ActionResult Index()
        {
            var categories = db.LOAISANPHAMs.OrderBy(x => x.TenLoai)
                .Select(x => new CategoryViewModel
                {
                    MaLoai = x.MaLoai,
                    TenLoai = x.TenLoai,
                    MoTa = x.MoTa,
                    TrangThai = x.TrangThai
                }).ToList();
            return View(categories);
        }

        public ActionResult Create()
        {
            return View(new CategoryViewModel { TrangThai = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CategoryViewModel model)
        {
            Normalize(model);
            if (ModelState.IsValid)
            {
                if (db.LOAISANPHAMs.Any(x => x.MaLoai == model.MaLoai))
                    ModelState.AddModelError("MaLoai", "Mã loại đã tồn tại.");
                ValidateName(model.TenLoai, null);
                if (ModelState.IsValid)
                {
                    db.LOAISANPHAMs.Add(new LOAISANPHAM
                    {
                        MaLoai = model.MaLoai,
                        TenLoai = model.TenLoai,
                        MoTa = model.MoTa,
                        TrangThai = model.TrangThai
                    });
                    if (TrySave())
                    {
                        TempData["CategorySuccess"] = "Đã thêm loại sản phẩm.";
                        return RedirectToAction("Index");
                    }
                }
            }
            return View(model);
        }

        public ActionResult Edit(string id)
        {
            var category = string.IsNullOrWhiteSpace(id) ? null : db.LOAISANPHAMs.Find(id);
            if (category == null) return HttpNotFound("Không tìm thấy loại sản phẩm.");

            ViewBag.CategoryId = category.MaLoai;
            return View(new CategoryEditViewModel
            {
                TenLoai = category.TenLoai,
                MoTa = category.MoTa,
                TrangThai = category.TrangThai
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string id, CategoryEditViewModel model)
        {
            var category = string.IsNullOrWhiteSpace(id) ? null : db.LOAISANPHAMs.Find(id);
            if (category == null) return HttpNotFound("Không tìm thấy loại sản phẩm.");

            ViewBag.CategoryId = category.MaLoai;
            Normalize(model);
            if (ModelState.IsValid)
            {
                ValidateName(model.TenLoai, category.MaLoai);
                if (ModelState.IsValid)
                {
                    category.TenLoai = model.TenLoai;
                    category.MoTa = model.MoTa;
                    category.TrangThai = model.TrangThai;
                    if (TrySave())
                    {
                        TempData["CategorySuccess"] = "Đã cập nhật loại sản phẩm.";
                        return RedirectToAction("Index");
                    }
                }
            }
            return View(model);
        }

        private static void Normalize(CategoryEditViewModel model)
        {
            model.TenLoai = model.TenLoai == null ? null : model.TenLoai.Trim();
            model.MoTa = string.IsNullOrWhiteSpace(model.MoTa) ? null : model.MoTa.Trim();
            var create = model as CategoryViewModel;
            if (create != null) create.MaLoai = create.MaLoai == null ? null : create.MaLoai.Trim();
        }

        private void ValidateName(string name, string excludedId)
        {
            // Use the database collation, matching its unique constraint (including inactive rows).
            if (db.LOAISANPHAMs.Any(x => x.TenLoai == name && (excludedId == null || x.MaLoai != excludedId)))
                ModelState.AddModelError("TenLoai", "Tên loại đã tồn tại, kể cả loại đã ngừng sử dụng.");
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
                    ? "Không thể lưu vì mã hoặc tên loại đã tồn tại. Vui lòng kiểm tra lại."
                    : "Không thể lưu loại sản phẩm. Vui lòng thử lại sau.");
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

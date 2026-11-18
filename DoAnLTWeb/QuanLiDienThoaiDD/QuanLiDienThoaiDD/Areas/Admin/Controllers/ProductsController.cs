using System;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using QuanLiDienThoaiDD.Areas.Admin.ViewModels;
using QuanLiDienThoaiDD.Models;

namespace QuanLiDienThoaiDD.Areas.Admin.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class ProductsController : Controller
    {
        private readonly QLHANGHOA_PHUKIEN_WEBEntities db = new QLHANGHOA_PHUKIEN_WEBEntities();

        public ActionResult Index()
        {
            return View(db.SANPHAMs.OrderBy(x => x.TenSanPham).Select(x => new ProductViewModel
            {
                MaSanPham = x.MaSanPham, TenSanPham = x.TenSanPham, MaLoai = x.MaLoai,
                MaThuongHieu = x.MaThuongHieu, MaNCCMacDinh = x.MaNCCMacDinh,
                MoTa = x.MoTa, BaoHanhThang = x.BaoHanhThang, TrangThai = x.TrangThai
            }).ToList());
        }

        public ActionResult Create()
        {
            Choices(null);
            return View(new ProductViewModel { BaoHanhThang = 12, TrangThai = true });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(ProductViewModel model)
        {
            Normalize(model);
            if (ModelState.IsValid)
            {
                if (db.SANPHAMs.Any(x => x.MaSanPham == model.MaSanPham))
                    ModelState.AddModelError("MaSanPham", "Mã sản phẩm đã tồn tại.");
                ValidateChoices(model, null);
                if (ModelState.IsValid)
                {
                    var product = new SANPHAM { MaSanPham = model.MaSanPham, NgayTao = DateTime.Now };
                    Apply(product, model);
                    db.SANPHAMs.Add(product);
                    if (TrySave()) return RedirectToAction("Index");
                }
            }
            Choices(null);
            return View(model);
        }

        public ActionResult Edit(string id)
        {
            var product = string.IsNullOrWhiteSpace(id) ? null : db.SANPHAMs.Find(id);
            if (product == null) return HttpNotFound("Không tìm thấy sản phẩm.");
            Choices(product);
            ViewBag.ProductId = product.MaSanPham;
            return View(new ProductEditViewModel
            {
                TenSanPham = product.TenSanPham, MaLoai = product.MaLoai, MaThuongHieu = product.MaThuongHieu,
                MaNCCMacDinh = product.MaNCCMacDinh, MoTa = product.MoTa,
                BaoHanhThang = product.BaoHanhThang, TrangThai = product.TrangThai
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(string id, ProductEditViewModel model)
        {
            var product = string.IsNullOrWhiteSpace(id) ? null : db.SANPHAMs.Find(id);
            if (product == null) return HttpNotFound("Không tìm thấy sản phẩm.");
            ViewBag.ProductId = product.MaSanPham;
            Normalize(model);
            if (ModelState.IsValid)
            {
                ValidateChoices(model, product);
                if (ModelState.IsValid)
                {
                    Apply(product, model);
                    if (TrySave()) return RedirectToAction("Index");
                }
            }
            Choices(product);
            return View(model);
        }

        private void ValidateChoices(ProductEditViewModel model, SANPHAM current)
        {
            var category = current == null ? null : current.MaLoai;
            var brand = current == null ? null : current.MaThuongHieu;
            var supplier = current == null ? null : current.MaNCCMacDinh;
            // Retain an existing inactive link; use SQL collation for all key comparisons.
            if (!db.LOAISANPHAMs.Any(x => x.MaLoai == model.MaLoai && (x.TrangThai || x.MaLoai == category)))
                ModelState.AddModelError("MaLoai", "Chọn loại đang sử dụng hoặc giữ nguyên liên kết cũ.");
            if (!db.THUONGHIEUx.Any(x => x.MaThuongHieu == model.MaThuongHieu && (x.TrangThai || x.MaThuongHieu == brand)))
                ModelState.AddModelError("MaThuongHieu", "Chọn thương hiệu đang sử dụng hoặc giữ nguyên liên kết cũ.");
            if (model.MaNCCMacDinh != null && !db.NHACUNGCAPs.Any(x => x.MaNCC == model.MaNCCMacDinh && (x.TrangThai || x.MaNCC == supplier)))
                ModelState.AddModelError("MaNCCMacDinh", "Chọn nhà cung cấp đang sử dụng, giữ nguyên liên kết cũ hoặc để trống.");
        }

        private void Choices(SANPHAM current)
        {
            var category = current == null ? null : current.MaLoai;
            var brand = current == null ? null : current.MaThuongHieu;
            var supplier = current == null ? null : current.MaNCCMacDinh;
            ViewBag.CategoryOptions = db.LOAISANPHAMs.Where(x => x.TrangThai || x.MaLoai == category).OrderBy(x => x.TenLoai).ToList()
                .Select(x => new SelectListItem { Value = x.MaLoai, Text = x.TenLoai + (x.TrangThai ? "" : " (ngừng sử dụng — liên kết cũ)") }).ToList();
            ViewBag.BrandOptions = db.THUONGHIEUx.Where(x => x.TrangThai || x.MaThuongHieu == brand).OrderBy(x => x.TenThuongHieu).ToList()
                .Select(x => new SelectListItem { Value = x.MaThuongHieu, Text = x.TenThuongHieu + (x.TrangThai ? "" : " (ngừng sử dụng — liên kết cũ)") }).ToList();
            ViewBag.SupplierOptions = db.NHACUNGCAPs.Where(x => x.TrangThai || x.MaNCC == supplier).OrderBy(x => x.TenNCC).ToList()
                .Select(x => new SelectListItem { Value = x.MaNCC, Text = x.TenNCC + (x.TrangThai ? "" : " (ngừng sử dụng — liên kết cũ)") }).ToList();
        }

        private static void Normalize(ProductEditViewModel model)
        {
            model.TenSanPham = model.TenSanPham == null ? null : model.TenSanPham.Trim();
            model.MaLoai = model.MaLoai == null ? null : model.MaLoai.Trim();
            model.MaThuongHieu = model.MaThuongHieu == null ? null : model.MaThuongHieu.Trim();
            model.MaNCCMacDinh = string.IsNullOrWhiteSpace(model.MaNCCMacDinh) ? null : model.MaNCCMacDinh.Trim();
            model.MoTa = string.IsNullOrWhiteSpace(model.MoTa) ? null : model.MoTa.Trim();
            var create = model as ProductViewModel;
            if (create != null) create.MaSanPham = create.MaSanPham == null ? null : create.MaSanPham.Trim();
        }

        private static void Apply(SANPHAM product, ProductEditViewModel model)
        {
            product.TenSanPham = model.TenSanPham;
            product.MaLoai = model.MaLoai;
            product.MaThuongHieu = model.MaThuongHieu;
            product.MaNCCMacDinh = model.MaNCCMacDinh;
            product.MoTa = model.MoTa;
            product.BaoHanhThang = model.BaoHanhThang.Value;
            product.TrangThai = model.TrangThai;
            product.NgayCapNhat = DateTime.Now;
        }

        private bool TrySave()
        {
            try { db.SaveChanges(); TempData["ProductSuccess"] = "Đã lưu sản phẩm."; return true; }
            catch (DbUpdateConcurrencyException)
            { ModelState.AddModelError("", "Sản phẩm đã thay đổi hoặc không còn tồn tại. Vui lòng tải lại."); }
            catch (DbUpdateException ex)
            {
                var sql = ex.GetBaseException() as SqlException;
                ModelState.AddModelError("", sql != null && (sql.Number == 2601 || sql.Number == 2627)
                    ? "Mã sản phẩm đã tồn tại. Vui lòng kiểm tra lại."
                    : "Không thể lưu sản phẩm. Danh mục có thể đã thay đổi; vui lòng tải lại hoặc thử lại sau.");
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

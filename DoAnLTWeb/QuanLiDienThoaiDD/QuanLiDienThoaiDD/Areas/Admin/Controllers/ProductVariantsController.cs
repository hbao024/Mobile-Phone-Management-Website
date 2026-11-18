using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using QuanLiDienThoaiDD.Areas.Admin.ViewModels;
using QuanLiDienThoaiDD.Models;

namespace QuanLiDienThoaiDD.Areas.Admin.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class ProductVariantsController : Controller
    {
        private readonly QLHANGHOA_PHUKIEN_WEBEntities db = new QLHANGHOA_PHUKIEN_WEBEntities();

        private bool LoadProduct(string productId)
        {
            var product = string.IsNullOrWhiteSpace(productId) ? null : db.SANPHAMs.Find(productId);
            if (product == null) return false;
            ViewBag.ProductId = product.MaSanPham;
            ViewBag.ProductName = product.TenSanPham;
            ViewBag.ProductActive = product.TrangThai;
            return true;
        }

        public ActionResult Index(string productId)
        {
            if (!LoadProduct(productId)) return HttpNotFound("Không tìm thấy sản phẩm.");
            return View(db.BIENSANPHAMs.Where(x => x.MaSanPham == productId).OrderBy(x => x.SKU)
                .Select(x => new ProductVariantViewModel
                {
                    MaBienSanPham = x.MaBienSanPham, SKU = x.SKU, MauSac = x.MauSac, RAMGB = x.RAMGB,
                    BoNhoGB = x.BoNhoGB, GiaNhap = x.GiaNhap, GiaBan = x.GiaBan,
                    HinhAnhUrl = x.HinhAnhUrl, TrangThai = x.TrangThai
                }).ToList());
        }

        public ActionResult Create(string productId)
        {
            if (!LoadProduct(productId)) return HttpNotFound("Không tìm thấy sản phẩm.");
            return View(new ProductVariantViewModel { TrangThai = true });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(string productId, ProductVariantViewModel model)
        {
            if (!LoadProduct(productId)) return HttpNotFound("Không tìm thấy sản phẩm.");
            Normalize(model);
            if (ModelState.IsValid)
            {
                if (db.BIENSANPHAMs.Any(x => x.MaBienSanPham == model.MaBienSanPham))
                    ModelState.AddModelError("MaBienSanPham", "Mã biến thể đã tồn tại.");
                ValidateSku(model.SKU, null);
                if (ModelState.IsValid)
                {
                    var variant = new BIENSANPHAM { MaBienSanPham = model.MaBienSanPham, MaSanPham = (string)ViewBag.ProductId };
                    Apply(variant, model);
                    // Stock is owned by the inventory flow, outside CAT-04.
                    db.BIENSANPHAMs.Add(variant);
                    if (TrySave()) return RedirectToAction("Index", new { productId = (string)ViewBag.ProductId });
                }
            }
            return View(model);
        }

        public ActionResult Edit(string productId, string id)
        {
            if (!LoadProduct(productId)) return HttpNotFound("Không tìm thấy sản phẩm.");
            var variant = FindVariant(productId, id);
            if (variant == null) return HttpNotFound("Không tìm thấy biến thể thuộc sản phẩm.");
            ViewBag.VariantId = variant.MaBienSanPham;
            return View(new ProductVariantEditViewModel
            {
                SKU = variant.SKU, MauSac = variant.MauSac, RAMGB = variant.RAMGB, BoNhoGB = variant.BoNhoGB,
                GiaNhap = variant.GiaNhap, GiaBan = variant.GiaBan, HinhAnhUrl = variant.HinhAnhUrl, TrangThai = variant.TrangThai
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(string productId, string id, ProductVariantEditViewModel model)
        {
            if (!LoadProduct(productId)) return HttpNotFound("Không tìm thấy sản phẩm.");
            var variant = FindVariant(productId, id);
            if (variant == null) return HttpNotFound("Không tìm thấy biến thể thuộc sản phẩm.");
            ViewBag.VariantId = variant.MaBienSanPham;
            Normalize(model);
            if (ModelState.IsValid)
            {
                ValidateSku(model.SKU, variant.MaBienSanPham);
                if (ModelState.IsValid)
                {
                    Apply(variant, model);
                    if (TrySave()) return RedirectToAction("Index", new { productId = (string)ViewBag.ProductId });
                }
            }
            return View(model);
        }

        private BIENSANPHAM FindVariant(string productId, string id)
        {
            return string.IsNullOrWhiteSpace(id) ? null
                : db.BIENSANPHAMs.SingleOrDefault(x => x.MaBienSanPham == id && x.MaSanPham == productId);
        }

        private void ValidateSku(string sku, string excludedId)
        {
            if (db.BIENSANPHAMs.Any(x => x.SKU == sku && (excludedId == null || x.MaBienSanPham != excludedId)))
                ModelState.AddModelError("SKU", "SKU đã tồn tại, kể cả ở sản phẩm khác hoặc biến thể ngừng kinh doanh.");
        }

        private static void Normalize(ProductVariantEditViewModel model)
        {
            model.SKU = model.SKU == null ? null : model.SKU.Trim();
            model.MauSac = string.IsNullOrWhiteSpace(model.MauSac) ? null : model.MauSac.Trim();
            model.HinhAnhUrl = string.IsNullOrWhiteSpace(model.HinhAnhUrl) ? null : model.HinhAnhUrl.Trim();
            var create = model as ProductVariantViewModel;
            if (create != null) create.MaBienSanPham = create.MaBienSanPham == null ? null : create.MaBienSanPham.Trim();
        }

        private static void Apply(BIENSANPHAM variant, ProductVariantEditViewModel model)
        {
            variant.SKU = model.SKU;
            variant.MauSac = model.MauSac;
            variant.RAMGB = model.RAMGB;
            variant.BoNhoGB = model.BoNhoGB;
            variant.GiaNhap = model.GiaNhap.Value;
            variant.GiaBan = model.GiaBan.Value;
            variant.HinhAnhUrl = model.HinhAnhUrl;
            variant.TrangThai = model.TrangThai;
        }

        private bool TrySave()
        {
            try { db.SaveChanges(); TempData["VariantSuccess"] = "Đã lưu biến thể."; return true; }
            catch (DbUpdateConcurrencyException)
            { ModelState.AddModelError("", "Biến thể đã thay đổi hoặc không còn tồn tại. Vui lòng tải lại."); }
            catch (DbUpdateException ex)
            {
                var sql = ex.GetBaseException() as SqlException;
                ModelState.AddModelError("", sql != null && (sql.Number == 2601 || sql.Number == 2627)
                    ? "Mã biến thể hoặc SKU đã tồn tại. Vui lòng kiểm tra lại."
                    : "Không thể lưu biến thể. Sản phẩm có thể đã thay đổi; vui lòng tải lại hoặc thử lại sau.");
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

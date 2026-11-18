using System;
using System.Data.Entity;
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

        public ActionResult Index(string q = null, string category = null, string brand = null,
            string state = null, string sort = null, string page = null)
        {
            q = (q ?? "").Trim();
            category = (category ?? "").Trim();
            brand = (brand ?? "").Trim();
            if (q.Length > 200 || category.Length > 10 || brand.Length > 10)
                return new HttpStatusCodeResult(400, "Invalid filter length.");
            var adjusted = false;
            if (state != null && state != "" && state != "active" && state != "inactive") { state = ""; adjusted = true; }
            if (string.IsNullOrEmpty(sort)) sort = "name";
            if (!new[] { "name", "name_desc", "newest", "oldest", "code" }.Contains(sort)) { sort = "name"; adjusted = true; }
            int current;
            if (!int.TryParse(page ?? "1", out current) || current < 1) { current = 1; adjusted = true; }
            var query = ProductQuery(q, category, brand, state, sort);
            var total = query.Count();
            const int size = 20;
            var pages = Math.Max(1, total / size + (total % size == 0 ? 0 : 1));
            if (current > pages) { current = pages; adjusted = true; }
            ViewBag.Search = q; ViewBag.Category = category; ViewBag.Brand = brand;
            ViewBag.State = state; ViewBag.Sort = sort; ViewBag.Page = current;
            ViewBag.Pages = pages; ViewBag.Total = total; ViewBag.Adjusted = adjusted;
            var categories = db.LOAISANPHAMs.OrderBy(x => x.TenLoai).ToList()
                .Select(x => new SelectListItem { Value = x.MaLoai, Text = x.TenLoai + (x.TrangThai ? "" : " (ngừng sử dụng)"), Selected = x.MaLoai == category }).ToList();
            var brands = db.THUONGHIEUx.OrderBy(x => x.TenThuongHieu).ToList()
                .Select(x => new SelectListItem { Value = x.MaThuongHieu, Text = x.TenThuongHieu + (x.TrangThai ? "" : " (ngừng sử dụng)"), Selected = x.MaThuongHieu == brand }).ToList();
            if (category != "" && !categories.Any(x => x.Value == category)) categories.Add(new SelectListItem { Value = category, Text = "Không tìm thấy: " + category, Selected = true });
            if (brand != "" && !brands.Any(x => x.Value == brand)) brands.Add(new SelectListItem { Value = brand, Text = "Không tìm thấy: " + brand, Selected = true });
            ViewBag.FilterCategories = categories; ViewBag.FilterBrands = brands;
            return View(query.Skip((current - 1) * size).Take(size).Select(x => new ProductViewModel
            {
                MaSanPham = x.MaSanPham, TenSanPham = x.TenSanPham, MaLoai = x.MaLoai,
                MaThuongHieu = x.MaThuongHieu, MaNCCMacDinh = x.MaNCCMacDinh,
                MoTa = x.MoTa, BaoHanhThang = x.BaoHanhThang, TrangThai = x.TrangThai
            }).ToList());
        }

        private IQueryable<SANPHAM> ProductQuery(string q, string category, string brand, string state, string sort)
        {
            var query = db.SANPHAMs.AsNoTracking().AsQueryable();
            if (q != "") query = query.Where(x => x.MaSanPham.Contains(q) || x.TenSanPham.Contains(q)
                || db.BIENSANPHAMs.Any(v => v.MaSanPham == x.MaSanPham && v.SKU.Contains(q)));
            if (category != "") query = query.Where(x => x.MaLoai == category);
            if (brand != "") query = query.Where(x => x.MaThuongHieu == brand);
            if (state == "active") query = query.Where(x => x.TrangThai);
            if (state == "inactive") query = query.Where(x => !x.TrangThai);
            switch (sort)
            {
                case "name_desc": return query.OrderByDescending(x => x.TenSanPham).ThenBy(x => x.MaSanPham);
                case "newest": return query.OrderByDescending(x => x.NgayTao).ThenBy(x => x.MaSanPham);
                case "oldest": return query.OrderBy(x => x.NgayTao).ThenBy(x => x.MaSanPham);
                case "code": return query.OrderBy(x => x.MaSanPham);
                default: return query.OrderBy(x => x.TenSanPham).ThenBy(x => x.MaSanPham);
            }
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

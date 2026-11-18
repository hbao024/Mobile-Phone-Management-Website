using System;
using System.Data.Entity;
using System.Linq;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Web.Security;
using System.Web.Mvc;
using QuanLiDienThoaiDD.Areas.Admin.ViewModels;
using QuanLiDienThoaiDD.Models;

namespace QuanLiDienThoaiDD.Areas.Admin.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class InventoryController : Controller
    {
        private readonly QLHANGHOA_PHUKIEN_WEBEntities db = new QLHANGHOA_PHUKIEN_WEBEntities();

        [HttpGet]
        public ActionResult Adjust(string id)
        {
            if (!LoadVariant(id)) return HttpNotFound();
            return View(new InventoryAdjustmentViewModel { Type = "DIEU_CHINH_TANG", OperationToken = Convert.ToBase64String(
                MachineKey.Protect(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N")), "InventoryAdjustment", User.Identity.Name, id)) });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Adjust(string id, InventoryAdjustmentViewModel model)
        {
            if (!LoadVariant(id)) return HttpNotFound();
            model.Reason = (model.Reason ?? "").Trim();
            if (model.Reason.Length == 0) ModelState.AddModelError("Reason", "Vui lòng nhập lý do.");
            Guid operation = Guid.Empty;
            if (ModelState.IsValid)
            {
                try
                {
                    var bytes = MachineKey.Unprotect(Convert.FromBase64String(model.OperationToken), "InventoryAdjustment", User.Identity.Name, id);
                    if (bytes == null || !Guid.TryParseExact(Encoding.UTF8.GetString(bytes), "N", out operation) || operation == Guid.Empty)
                        ModelState.AddModelError("", "Mã thao tác không hợp lệ. Vui lòng tải lại form.");
                }
                catch (FormatException) { ModelState.AddModelError("", "Mã thao tác không hợp lệ."); }
                catch (CryptographicException) { ModelState.AddModelError("", "Mã thao tác không hợp lệ. Vui lòng tải lại form."); }
            }
            if (ModelState.IsValid)
            {
                var name = User.Identity.Name;
                var staff = db.NHANVIENs.AsNoTracking().Where(x => x.TrangThai && x.TAIKHOAN.TrangThai
                    && x.TAIKHOAN.TenDangNhap == name && x.TAIKHOAN.MaVaiTro == "ADMIN").Select(x => x.MaNV).Take(2).ToList();
                if (staff.Count != 1) ModelState.AddModelError("", "Tài khoản cần liên kết đúng một nhân viên đang hoạt động để ghi nhật ký kho.");
                else
                {
                    try
                    {
                        var result = ExecuteChange(new InventoryChange { OperationId = operation, VariantId = id, Type = model.Type,
                            Quantity = model.Quantity.Value, StaffId = staff[0], Reason = model.Reason });
                        TempData["InventorySuccess"] = result.Replayed ? "Thao tác này đã được ghi trước đó; không áp dụng thêm." : "Đã điều chỉnh tồn kho và ghi nhật ký.";
                        return RedirectToAction("History", new { id });
                    }
                    catch (SqlException ex)
                    {
                        ModelState.AddModelError("", ex.Number == 50006 || ex.Number == 50007 ? "Chưa có tồn, tồn không đủ để giảm hoặc tăng vượt giới hạn số nguyên."
                            : ex.Number == 50003 ? "Mã thao tác đã dùng với nội dung khác. Kiểm tra lịch sử và tải form mới nếu cần điều chỉnh khác."
                            : "Không xác nhận được kết quả lưu. Kiểm tra lịch sử; nếu thử lại hãy giữ nguyên nội dung và mã thao tác.");
                        Trace.TraceError("Inventory adjustment failed: SQL {0}.", ex.Number);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Inventory adjustment failed ({0}).", ex.GetType().Name);
                        ModelState.AddModelError("", "Không xác nhận được kết quả lưu. Kiểm tra lịch sử; thử lại cùng nội dung và mã thao tác khi cần.");
                    }
                }
            }
            return View(model);
        }

        protected virtual InventoryResult ExecuteChange(InventoryChange change)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                var result = new InventoryService().Apply(transaction.UnderlyingTransaction, change);
                transaction.Commit();
                return result;
            }
        }

        [HttpGet]
        public ActionResult History(string id, string page = null)
        {
            if (!LoadVariant(id)) return HttpNotFound();
            var query = db.GIAODICHKHOes.AsNoTracking().Where(x => x.MaBienSanPham == id
                && (x.LoaiGiaoDich == "DIEU_CHINH_TANG" || x.LoaiGiaoDich == "DIEU_CHINH_GIAM"));
            var total = query.Count();
            var pages = Math.Max(1, total / 20 + (total % 20 == 0 ? 0 : 1));
            int current;
            if (!int.TryParse(page ?? "1", out current)) current = 1;
            current = Math.Max(1, Math.Min(current, pages));
            ViewBag.Page = current; ViewBag.Pages = pages; ViewBag.Total = total;
            return View(query.OrderByDescending(x => x.NgayGiaoDich).ThenByDescending(x => x.MaGiaoDich)
                .Skip((current - 1) * 20).Take(20).ToList());
        }

        private bool LoadVariant(string id)
        {
            var variant = string.IsNullOrWhiteSpace(id) ? null : db.BIENSANPHAMs.AsNoTracking().SingleOrDefault(x => x.MaBienSanPham == id);
            if (variant == null) return false;
            ViewBag.VariantId = variant.MaBienSanPham; ViewBag.SKU = variant.SKU;
            return true;
        }

        [HttpGet]
        public ActionResult Index(string q = null, string state = null, string productState = null, string stock = null, string page = null)
        {
            q = (q ?? "").Trim();
            if (q.Length > 200) return new HttpStatusCodeResult(400, "Invalid search length.");
            var adjusted = false;
            if (!new[] { null, "", "active", "inactive" }.Contains(state)) { state = ""; adjusted = true; }
            if (!new[] { null, "", "active", "inactive" }.Contains(productState)) { productState = ""; adjusted = true; }
            if (!new[] { null, "", "missing", "present" }.Contains(stock)) { stock = ""; adjusted = true; }
            int current;
            if (!int.TryParse(page ?? "1", out current) || current < 1) { current = 1; adjusted = true; }
            var query = InventoryQuery(q, state, productState, stock);
            var total = query.Count();
            var pages = Math.Max(1, total / 20 + (total % 20 == 0 ? 0 : 1));
            if (current > pages) { current = pages; adjusted = true; }
            ViewBag.Search = q; ViewBag.State = state; ViewBag.ProductState = productState; ViewBag.Stock = stock;
            ViewBag.Page = current; ViewBag.Pages = pages; ViewBag.Total = total; ViewBag.Adjusted = adjusted;
            return View(query.OrderBy(x => x.TenSanPham).ThenBy(x => x.MaSanPham).ThenBy(x => x.MaBienSanPham)
                .Skip((current - 1) * 20).Take(20).ToList());
        }

        private IQueryable<InventoryRowViewModel> InventoryQuery(string q, string state, string productState, string stock)
        {
            var query = from v in db.BIENSANPHAMs.AsNoTracking()
                        join p in db.SANPHAMs.AsNoTracking() on v.MaSanPham equals p.MaSanPham
                        join t in db.TONKHOes.AsNoTracking() on v.MaBienSanPham equals t.MaBienSanPham into stocks
                        from t in stocks.DefaultIfEmpty()
                        select new InventoryRowViewModel
                        {
                            MaBienSanPham = v.MaBienSanPham, SKU = v.SKU, MaSanPham = p.MaSanPham,
                            TenSanPham = p.TenSanPham, VariantActive = v.TrangThai, ProductActive = p.TrangThai,
                            SoLuongTon = t == null ? (int?)null : t.SoLuongTon,
                            SoLuongDangDat = t == null ? (int?)null : t.SoLuongDangDat,
                            NgayCapNhat = t == null ? (DateTime?)null : t.NgayCapNhat
                        };
            if (q != "") query = query.Where(x => x.MaBienSanPham.Contains(q) || x.SKU.Contains(q)
                || x.MaSanPham.Contains(q) || x.TenSanPham.Contains(q));
            if (state == "active") query = query.Where(x => x.VariantActive);
            if (state == "inactive") query = query.Where(x => !x.VariantActive);
            if (productState == "active") query = query.Where(x => x.ProductActive);
            if (productState == "inactive") query = query.Where(x => !x.ProductActive);
            if (stock == "missing") query = query.Where(x => !x.SoLuongTon.HasValue);
            if (stock == "present") query = query.Where(x => x.SoLuongTon.HasValue);
            return query;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}

using System;
using System.Data.Entity;
using System.Linq;
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

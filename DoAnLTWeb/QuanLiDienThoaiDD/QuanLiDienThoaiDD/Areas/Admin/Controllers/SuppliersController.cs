using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using QuanLiDienThoaiDD.Areas.Admin.ViewModels;
using QuanLiDienThoaiDD.Models;

namespace QuanLiDienThoaiDD.Areas.Admin.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class SuppliersController : Controller
    {
        private readonly QLHANGHOA_PHUKIEN_WEBEntities db = new QLHANGHOA_PHUKIEN_WEBEntities();

        public ActionResult Index()
        {
            var suppliers = db.NHACUNGCAPs.OrderBy(x => x.TenNCC)
                .Select(x => new SupplierViewModel
                {
                    MaNCC = x.MaNCC,
                    TenNCC = x.TenNCC,
                    DiaChi = x.DiaChi,
                    DienThoai = x.DienThoai,
                    Email = x.Email,
                    TrangThai = x.TrangThai
                }).ToList();
            return View(suppliers);
        }

        public ActionResult Create()
        {
            return View(new SupplierViewModel { TrangThai = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(SupplierViewModel model)
        {
            Normalize(model);
            if (ModelState.IsValid)
            {
                if (db.NHACUNGCAPs.Any(x => x.MaNCC == model.MaNCC))
                    ModelState.AddModelError("MaNCC", "Mã nhà cung cấp đã tồn tại.");
                if (ModelState.IsValid)
                {
                    db.NHACUNGCAPs.Add(new NHACUNGCAP
                    {
                        MaNCC = model.MaNCC,
                        TenNCC = model.TenNCC,
                        DiaChi = model.DiaChi,
                        DienThoai = model.DienThoai,
                        Email = model.Email,
                        TrangThai = model.TrangThai
                    });
                    if (TrySave())
                    {
                        TempData["SupplierSuccess"] = "Đã thêm nhà cung cấp.";
                        return RedirectToAction("Index");
                    }
                }
            }
            return View(model);
        }

        public ActionResult Edit(string id)
        {
            var supplier = string.IsNullOrWhiteSpace(id) ? null : db.NHACUNGCAPs.Find(id);
            if (supplier == null) return HttpNotFound("Không tìm thấy nhà cung cấp.");

            ViewBag.SupplierId = supplier.MaNCC;
            return View(new SupplierEditViewModel
            {
                TenNCC = supplier.TenNCC,
                DiaChi = supplier.DiaChi,
                DienThoai = supplier.DienThoai,
                Email = supplier.Email,
                TrangThai = supplier.TrangThai
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string id, SupplierEditViewModel model)
        {
            var supplier = string.IsNullOrWhiteSpace(id) ? null : db.NHACUNGCAPs.Find(id);
            if (supplier == null) return HttpNotFound("Không tìm thấy nhà cung cấp.");

            ViewBag.SupplierId = supplier.MaNCC;
            Normalize(model);
            if (ModelState.IsValid)
            {
                supplier.TenNCC = model.TenNCC;
                supplier.DiaChi = model.DiaChi;
                supplier.DienThoai = model.DienThoai;
                supplier.Email = model.Email;
                supplier.TrangThai = model.TrangThai;
                if (TrySave())
                {
                    TempData["SupplierSuccess"] = "Đã cập nhật nhà cung cấp.";
                    return RedirectToAction("Index");
                }
            }
            return View(model);
        }

        private static void Normalize(SupplierEditViewModel model)
        {
            model.TenNCC = model.TenNCC == null ? null : model.TenNCC.Trim();
            model.DiaChi = string.IsNullOrWhiteSpace(model.DiaChi) ? null : model.DiaChi.Trim();
            model.DienThoai = string.IsNullOrWhiteSpace(model.DienThoai) ? null : model.DienThoai.Trim();
            model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            var create = model as SupplierViewModel;
            if (create != null) create.MaNCC = create.MaNCC == null ? null : create.MaNCC.Trim();
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
                    ? "Không thể lưu vì mã nhà cung cấp đã tồn tại. Vui lòng kiểm tra lại."
                    : "Không thể lưu nhà cung cấp. Vui lòng thử lại sau.");
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

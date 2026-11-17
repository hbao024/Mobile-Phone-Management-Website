using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using QuanLiDienThoaiDD.Areas.Admin.ViewModels;
using QuanLiDienThoaiDD.Models;

namespace QuanLiDienThoaiDD.Areas.Admin.Controllers
{
    [Authorize(Roles = "ADMIN,NHANVIEN")]
    public class CustomersController : Controller
    {
        private const int PageSize = 10;

        public ActionResult Index(string search, string statusFilter, string accountFilter, int page = 1)
        {
            using (var db = new QLHANGHOA_PHUKIEN_WEBEntities())
            {
                var allCustomers = db.KHACHHANGs.AsQueryable();

                var model = new CustomerIndexViewModel
                {
                    Search = search,
                    StatusFilter = statusFilter,
                    AccountFilter = accountFilter,
                    TotalCustomers = allCustomers.Count(),
                    ActiveCustomers = allCustomers.Count(x => x.TrangThai),
                    LockedCustomers = allCustomers.Count(x => !x.TrangThai),
                    LinkedAccountCustomers = allCustomers.Count(x => x.MaTaiKhoan != null),
                    TotalCustomerSpending = db.DONHANGs
                        .Where(x => x.MaKH != null && x.TrangThaiDonHang != "DA_HUY")
                        .Select(x => (decimal?)x.TongTien)
                        .Sum() ?? 0m
                };

                var query = db.KHACHHANGs
                    .Include(x => x.TAIKHOAN)
                    .Include(x => x.DONHANGs)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var kw = search.Trim();
                    query = query.Where(x =>
                        x.MaKH.Contains(kw) ||
                        x.TenKH.Contains(kw) ||
                        x.DienThoai.Contains(kw) ||
                        (x.Email != null && x.Email.Contains(kw)) ||
                        (x.DiaChi != null && x.DiaChi.Contains(kw)));
                }

                if (statusFilter == "active")
                {
                    query = query.Where(x => x.TrangThai);
                }
                else if (statusFilter == "locked")
                {
                    query = query.Where(x => !x.TrangThai);
                }

                if (accountFilter == "linked")
                {
                    query = query.Where(x => x.MaTaiKhoan != null);
                }
                else if (accountFilter == "guest")
                {
                    query = query.Where(x => x.MaTaiKhoan == null);
                }

                var totalFiltered = query.Count();
                var totalPages = Math.Max(1, (int)Math.Ceiling(totalFiltered / (double)PageSize));
                page = Math.Max(1, Math.Min(page, totalPages));

                var customers = query
                    .OrderByDescending(x => x.NgayThamGia)
                    .ThenBy(x => x.MaKH)
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                model.CurrentPage = page;
                model.TotalPages = totalPages;
                model.Customers = customers.Select(x => new CustomerListItemViewModel
                {
                    MaKH = x.MaKH,
                    TenKH = x.TenKH,
                    Email = x.Email,
                    DienThoai = x.DienThoai,
                    DiaChi = x.DiaChi,
                    NgayThamGia = x.NgayThamGia,
                    MaTaiKhoan = x.MaTaiKhoan,
                    TenDangNhap = x.TAIKHOAN != null ? x.TAIKHOAN.TenDangNhap : null,
                    TrangThai = x.TrangThai,
                    SoDonHang = x.DONHANGs.Count,
                    SoDonThanhCong = x.DONHANGs.Count(dh => dh.TrangThaiDonHang == "DA_GIAO"),
                    TongChiTieu = x.DONHANGs
                        .Where(dh => dh.TrangThaiDonHang != "DA_HUY")
                        .Sum(dh => (decimal?)dh.TongTien) ?? 0m
                }).ToList();

                return View(model);
            }
        }

        public ActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index");
            }

            using (var db = new QLHANGHOA_PHUKIEN_WEBEntities())
            {
                var customer = db.KHACHHANGs
                    .Include(x => x.TAIKHOAN)
                    .Include(x => x.DONHANGs.Select(dh => dh.CHITIETDONHANGs))
                    .Include(x => x.DONHANGs.Select(dh => dh.NHANVIEN))
                    .Include(x => x.DANHGIAs.Select(dg => dg.BIENSANPHAM.SANPHAM))
                    .FirstOrDefault(x => x.MaKH == id);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy khách hàng có mã: " + id;
                    return RedirectToAction("Index");
                }

                var model = new CustomerDetailViewModel
                {
                    MaKH = customer.MaKH,
                    TenKH = customer.TenKH,
                    Email = customer.Email,
                    DienThoai = customer.DienThoai,
                    DiaChi = customer.DiaChi,
                    NgayThamGia = customer.NgayThamGia,
                    MaTaiKhoan = customer.MaTaiKhoan,
                    TenDangNhap = customer.TAIKHOAN != null ? customer.TAIKHOAN.TenDangNhap : null,
                    TrangThaiTaiKhoan = customer.TAIKHOAN != null ? (bool?)customer.TAIKHOAN.TrangThai : null,
                    TrangThai = customer.TrangThai,
                    TongSoDonHang = customer.DONHANGs.Count,
                    SoDonHoanThanh = customer.DONHANGs.Count(x => x.TrangThaiDonHang == "DA_GIAO"),
                    SoDonDangXuLy = customer.DONHANGs.Count(x =>
                        x.TrangThaiDonHang == "CHO_XAC_NHAN" ||
                        x.TrangThaiDonHang == "DA_XAC_NHAN" ||
                        x.TrangThaiDonHang == "DANG_GIAO"),
                    SoDonDaHuy = customer.DONHANGs.Count(x => x.TrangThaiDonHang == "DA_HUY"),
                    TongChiTieuHopLe = customer.DONHANGs
                        .Where(x => x.TrangThaiDonHang != "DA_HUY")
                        .Sum(x => (decimal?)x.TongTien) ?? 0m,
                    Orders = customer.DONHANGs
                        .OrderByDescending(x => x.NgayDat)
                        .Select(x => new OrderListItemViewModel
                        {
                            MaDonHang = x.MaDonHang,
                            MaKH = x.MaKH,
                            TenKhachHang = customer.TenKH,
                            TenNguoiNhan = x.TenNguoiNhan,
                            SoDienThoaiNhan = x.SoDienThoaiNhan,
                            DiaChiGiaoHang = x.DiaChiGiaoHang,
                            NgayDat = x.NgayDat,
                            NgayCapNhat = x.NgayCapNhat,
                            TrangThaiDonHang = x.TrangThaiDonHang,
                            PhuongThucThanhToan = x.PhuongThucThanhToan,
                            TrangThaiThanhToan = x.TrangThaiThanhToan,
                            TongSoSanPham = x.CHITIETDONHANGs.Sum(ct => (int?)ct.SoLuong) ?? 0,
                            TongTien = x.TongTien,
                            TenNVXuLy = x.NHANVIEN != null ? x.NHANVIEN.TenNV : "Chưa phân công"
                        })
                        .ToList(),
                    Reviews = customer.DANHGIAs
                        .OrderByDescending(x => x.NgayDanhGia)
                        .Select(x => new CustomerReviewItemViewModel
                        {
                            MaDanhGia = x.MaDanhGia,
                            MaDonHang = x.MaDonHang,
                            TenSanPham = x.BIENSANPHAM != null && x.BIENSANPHAM.SANPHAM != null
                                ? x.BIENSANPHAM.SANPHAM.TenSanPham
                                : x.MaBienSanPham,
                            SKU = x.BIENSANPHAM != null ? x.BIENSANPHAM.SKU : x.MaBienSanPham,
                            SoSao = x.SoSao,
                            NoiDung = x.NoiDung,
                            NgayDanhGia = x.NgayDanhGia,
                            TrangThai = x.TrangThai
                        })
                        .ToList()
                };

                return View(model);
            }
        }

        public ActionResult Create()
        {
            using (var db = new QLHANGHOA_PHUKIEN_WEBEntities())
            {
                var model = new CustomerFormViewModel
                {
                    MaKH = GenerateNextCustomerId(db),
                    TrangThai = true
                };
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CustomerFormViewModel model)
        {
            using (var db = new QLHANGHOA_PHUKIEN_WEBEntities())
            {
                var maKH = string.IsNullOrWhiteSpace(model.MaKH)
                    ? GenerateNextCustomerId(db)
                    : model.MaKH.Trim().ToUpperInvariant();

                if (db.KHACHHANGs.Any(x => x.MaKH == maKH))
                {
                    ModelState.AddModelError("MaKH", "Mã khách hàng đã tồn tại.");
                }

                var email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
                if (email != null && db.KHACHHANGs.Any(x => x.Email == email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi một khách hàng khác.");
                }

                if (!ModelState.IsValid)
                {
                    model.MaKH = maKH;
                    return View(model);
                }

                var customer = new KHACHHANG
                {
                    MaKH = maKH,
                    TenKH = model.TenKH.Trim(),
                    Email = email,
                    DienThoai = model.DienThoai.Trim(),
                    DiaChi = string.IsNullOrWhiteSpace(model.DiaChi) ? null : model.DiaChi.Trim(),
                    NgayThamGia = DateTime.Now,
                    TrangThai = model.TrangThai
                };

                db.KHACHHANGs.Add(customer);
                db.SaveChanges();

                TempData["SuccessMessage"] = string.Format("Đã thêm khách hàng {0} - {1} thành công.", customer.MaKH, customer.TenKH);
                return RedirectToAction("Details", new { id = customer.MaKH });
            }
        }

        public ActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index");
            }

            using (var db = new QLHANGHOA_PHUKIEN_WEBEntities())
            {
                var customer = db.KHACHHANGs.FirstOrDefault(x => x.MaKH == id);
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy khách hàng: " + id;
                    return RedirectToAction("Index");
                }

                var model = new CustomerFormViewModel
                {
                    MaKH = customer.MaKH,
                    TenKH = customer.TenKH,
                    Email = customer.Email,
                    DienThoai = customer.DienThoai,
                    DiaChi = customer.DiaChi,
                    TrangThai = customer.TrangThai
                };

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CustomerFormViewModel model)
        {
            using (var db = new QLHANGHOA_PHUKIEN_WEBEntities())
            {
                var customer = db.KHACHHANGs
                    .Include(x => x.TAIKHOAN)
                    .FirstOrDefault(x => x.MaKH == model.MaKH);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy khách hàng: " + model.MaKH;
                    return RedirectToAction("Index");
                }

                var email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
                if (email != null && db.KHACHHANGs.Any(x => x.Email == email && x.MaKH != customer.MaKH))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi một khách hàng khác.");
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                customer.TenKH = model.TenKH.Trim();
                customer.Email = email;
                customer.DienThoai = model.DienThoai.Trim();
                customer.DiaChi = string.IsNullOrWhiteSpace(model.DiaChi) ? null : model.DiaChi.Trim();
                customer.TrangThai = model.TrangThai;

                if (customer.TAIKHOAN != null && customer.TAIKHOAN.MaVaiTro == "KHACHHANG")
                {
                    customer.TAIKHOAN.TrangThai = model.TrangThai;
                }

                db.SaveChanges();

                TempData["SuccessMessage"] = string.Format("Đã cập nhật thông tin khách hàng {0} thành công.", customer.MaKH);
                return RedirectToAction("Details", new { id = customer.MaKH });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleStatus(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index");
            }

            using (var db = new QLHANGHOA_PHUKIEN_WEBEntities())
            {
                var customer = db.KHACHHANGs
                    .Include(x => x.TAIKHOAN)
                    .FirstOrDefault(x => x.MaKH == id);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy khách hàng: " + id;
                    return RedirectToAction("Index");
                }

                customer.TrangThai = !customer.TrangThai;
                if (customer.TAIKHOAN != null && customer.TAIKHOAN.MaVaiTro == "KHACHHANG")
                {
                    customer.TAIKHOAN.TrangThai = customer.TrangThai;
                }

                db.SaveChanges();

                TempData["SuccessMessage"] = string.Format(
                    "Đã {0} khách hàng {1} ({2}).",
                    customer.TrangThai ? "mở khóa" : "khóa",
                    customer.TenKH,
                    customer.MaKH);

                return RedirectToAction("Details", new { id = customer.MaKH });
            }
        }

        private static string GenerateNextCustomerId(QLHANGHOA_PHUKIEN_WEBEntities db)
        {
            var existingIds = db.KHACHHANGs
                .Where(x => x.MaKH.StartsWith("KH"))
                .Select(x => x.MaKH)
                .ToList();

            var maxNumber = 0;
            foreach (var id in existingIds)
            {
                int parsed;
                if (id.Length > 2 && int.TryParse(id.Substring(2), out parsed) && parsed > maxNumber)
                {
                    maxNumber = parsed;
                }
            }

            return string.Format("KH{0:000}", maxNumber + 1);
        }
    }
}

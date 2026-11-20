using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using QuanLiDienThoaiDD.Areas.Admin.ViewModels;
using QuanLiDienThoaiDD.Models;

namespace QuanLiDienThoaiDD.Areas.Admin.Controllers
{
    [Authorize(Roles = "ADMIN,NHANVIEN")]
    public class StockReceiptsController : Controller
    {
        private const int PageSize = 10;

        public ActionResult Index(
            string search,
            string maNCC,
            string status,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1)
        {
            using (var db = new QLHANGHOA_PHUKIEN_WEBEntities())
            {
                var allReceipts = db.PHIEUNHAPs.AsQueryable();

                var model = new StockReceiptIndexViewModel
                {
                    Search = search,
                    MaNCC = maNCC,
                    Status = status,
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalReceipts = allReceipts.Count(),
                    ImportedReceipts = allReceipts.Count(x => x.TrangThai == "DA_NHAP"),
                    PendingReceipts = allReceipts.Count(x => x.TrangThai == "CHO_XU_LY"),
                    CanceledReceipts = allReceipts.Count(x => x.TrangThai == "DA_HUY"),
                    TotalImportAmount = allReceipts
                        .Where(x => x.TrangThai == "DA_NHAP")
                        .Select(x => (decimal?)x.TongTien)
                        .Sum() ?? 0m,
                    TotalUnitsInStock = db.TONKHOes.Select(x => (int?)x.SoLuongTon).Sum() ?? 0,
                    Suppliers = db.NHACUNGCAPs
                        .OrderBy(x => x.TenNCC)
                        .Select(x => new SupplierOptionViewModel
                        {
                            MaNCC = x.MaNCC,
                            TenNCC = x.TenNCC
                        })
                        .ToList()
                };

                var query = db.PHIEUNHAPs
                    .Include(x => x.NHACUNGCAP)
                    .Include(x => x.NHANVIEN)
                    .Include(x => x.CHITIETPHIEUNHAPs)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var kw = search.Trim();
                    query = query.Where(x =>
                        x.MaPhieuNhap.Contains(kw) ||
                        x.NHACUNGCAP.TenNCC.Contains(kw) ||
                        x.NHANVIEN.TenNV.Contains(kw) ||
                        (x.GhiChu != null && x.GhiChu.Contains(kw)));
                }

                if (!string.IsNullOrWhiteSpace(maNCC))
                {
                    query = query.Where(x => x.MaNCC == maNCC);
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(x => x.TrangThai == status);
                }

                if (fromDate.HasValue)
                {
                    var start = fromDate.Value.Date;
                    query = query.Where(x => x.NgayNhap >= start);
                }

                if (toDate.HasValue)
                {
                    var endExclusive = toDate.Value.Date.AddDays(1);
                    query = query.Where(x => x.NgayNhap < endExclusive);
                }

                var totalFiltered = query.Count();
                var totalPages = Math.Max(1, (int)Math.Ceiling(totalFiltered / (double)PageSize));
                page = Math.Max(1, Math.Min(page, totalPages));

                var receipts = query
                    .OrderByDescending(x => x.NgayNhap)
                    .ThenByDescending(x => x.MaPhieuNhap)
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                model.CurrentPage = page;
                model.TotalPages = totalPages;
                model.Receipts = receipts.Select(x => new StockReceiptListItemViewModel
                {
                    MaPhieuNhap = x.MaPhieuNhap,
                    MaNCC = x.MaNCC,
                    TenNCC = x.NHACUNGCAP != null ? x.NHACUNGCAP.TenNCC : x.MaNCC,
                    MaNV = x.MaNV,
                    TenNV = x.NHANVIEN != null ? x.NHANVIEN.TenNV : x.MaNV,
                    NgayNhap = x.NgayNhap,
                    TrangThai = x.TrangThai,
                    SoDongSanPham = x.CHITIETPHIEUNHAPs.Count,
                    TongSoLuongNhap = x.CHITIETPHIEUNHAPs.Sum(ct => (int?)ct.SoLuong) ?? 0,
                    TongTien = x.TongTien,
                    GhiChu = x.GhiChu
                }).ToList();

                var variants = db.BIENSANPHAMs
                    .Include(x => x.SANPHAM.THUONGHIEU)
                    .Include(x => x.TONKHO)
                    .Where(x => x.TrangThai && x.SANPHAM.TrangThai)
                    .OrderBy(x => x.TONKHO != null ? x.TONKHO.SoLuongTon : 0)
                    .ThenBy(x => x.SANPHAM.TenSanPham)
                    .ToList();

                model.InventoryItems = variants.Select(x => new InventorySnapshotItemViewModel
                {
                    MaBienSanPham = x.MaBienSanPham,
                    SKU = x.SKU,
                    TenSanPham = x.SANPHAM != null ? x.SANPHAM.TenSanPham : x.MaSanPham,
                    TenThuongHieu = x.SANPHAM != null && x.SANPHAM.THUONGHIEU != null
                        ? x.SANPHAM.THUONGHIEU.TenThuongHieu
                        : "-",
                    CauHinh = FormatVariantSpecs(x),
                    GiaNhap = x.GiaNhap,
                    GiaBan = x.GiaBan,
                    SoLuongTon = x.TONKHO != null ? x.TONKHO.SoLuongTon : 0,
                    NgayCapNhat = x.TONKHO != null ? x.TONKHO.NgayCapNhat : DateTime.Now
                }).ToList();

                model.RecentLogs = db.GIAODICHKHOes
                    .Include(x => x.BIENSANPHAM)
                    .Include(x => x.NHANVIEN)
                    .OrderByDescending(x => x.NgayGiaoDich)
                    .ThenByDescending(x => x.MaGiaoDich)
                    .Take(10)
                    .ToList()
                    .Select(x => new OrderInventoryLogItemViewModel
                    {
                        MaGiaoDich = x.MaGiaoDich,
                        MaBienSanPham = x.MaBienSanPham,
                        SKU = x.BIENSANPHAM != null ? x.BIENSANPHAM.SKU : x.MaBienSanPham,
                        LoaiGiaoDich = x.LoaiGiaoDich,
                        SoLuong = x.SoLuong,
                        SoLuongTruoc = x.SoLuongTruoc,
                        SoLuongSau = x.SoLuongSau,
                        TenNV = x.NHANVIEN != null ? x.NHANVIEN.TenNV : x.MaNV,
                        NgayGiaoDich = x.NgayGiaoDich,
                        GhiChu = x.GhiChu
                    })
                    .ToList();

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
                var receipt = db.PHIEUNHAPs
                    .Include(x => x.NHACUNGCAP)
                    .Include(x => x.NHANVIEN)
                    .Include(x => x.CHITIETPHIEUNHAPs.Select(ct => ct.BIENSANPHAM.SANPHAM))
                    .Include(x => x.CHITIETPHIEUNHAPs.Select(ct => ct.BIENSANPHAM.TONKHO))
                    .Include(x => x.GIAODICHKHOes.Select(gd => gd.BIENSANPHAM))
                    .Include(x => x.GIAODICHKHOes.Select(gd => gd.NHANVIEN))
                    .FirstOrDefault(x => x.MaPhieuNhap == id);

                if (receipt == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy phiếu nhập: " + id;
                    return RedirectToAction("Index");
                }

                var model = new StockReceiptDetailViewModel
                {
                    MaPhieuNhap = receipt.MaPhieuNhap,
                    MaNCC = receipt.MaNCC,
                    TenNCC = receipt.NHACUNGCAP != null ? receipt.NHACUNGCAP.TenNCC : receipt.MaNCC,
                    DienThoaiNCC = receipt.NHACUNGCAP != null ? receipt.NHACUNGCAP.DienThoai : null,
                    DiaChiNCC = receipt.NHACUNGCAP != null ? receipt.NHACUNGCAP.DiaChi : null,
                    MaNV = receipt.MaNV,
                    TenNV = receipt.NHANVIEN != null ? receipt.NHANVIEN.TenNV : receipt.MaNV,
                    NgayNhap = receipt.NgayNhap,
                    TrangThai = receipt.TrangThai,
                    TongTien = receipt.TongTien,
                    GhiChu = receipt.GhiChu,
                    CanConfirm = receipt.TrangThai == "CHO_XU_LY",
                    CanCancel = receipt.TrangThai == "CHO_XU_LY",
                    Items = receipt.CHITIETPHIEUNHAPs
                        .OrderBy(x => x.MaBienSanPham)
                        .Select(x => new StockReceiptDetailItemViewModel
                        {
                            MaBienSanPham = x.MaBienSanPham,
                            SKU = x.BIENSANPHAM != null ? x.BIENSANPHAM.SKU : x.MaBienSanPham,
                            TenSanPham = x.BIENSANPHAM != null && x.BIENSANPHAM.SANPHAM != null
                                ? x.BIENSANPHAM.SANPHAM.TenSanPham
                                : x.MaBienSanPham,
                            CauHinh = FormatVariantSpecs(x.BIENSANPHAM),
                            DonGia = x.DonGia,
                            SoLuong = x.SoLuong,
                            ThanhTien = x.ThanhTien ?? (x.DonGia * x.SoLuong),
                            SoLuongTonHienTai = x.BIENSANPHAM != null && x.BIENSANPHAM.TONKHO != null
                                ? x.BIENSANPHAM.TONKHO.SoLuongTon
                                : 0
                        })
                        .ToList(),
                    InventoryLogs = receipt.GIAODICHKHOes
                        .OrderByDescending(x => x.NgayGiaoDich)
                        .ThenByDescending(x => x.MaGiaoDich)
                        .Select(x => new OrderInventoryLogItemViewModel
                        {
                            MaGiaoDich = x.MaGiaoDich,
                            MaBienSanPham = x.MaBienSanPham,
                            SKU = x.BIENSANPHAM != null ? x.BIENSANPHAM.SKU : x.MaBienSanPham,
                            LoaiGiaoDich = x.LoaiGiaoDich,
                            SoLuong = x.SoLuong,
                            SoLuongTruoc = x.SoLuongTruoc,
                            SoLuongSau = x.SoLuongSau,
                            TenNV = x.NHANVIEN != null ? x.NHANVIEN.TenNV : x.MaNV,
                            NgayGiaoDich = x.NgayGiaoDich,
                            GhiChu = x.GhiChu
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
                var model = new CreateStockReceiptViewModel
                {
                    MaPhieuNhap = GenerateNextReceiptId(db),
                    TrangThai = "DA_NHAP"
                };

                PopulateCreateOptions(db, model);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateStockReceiptViewModel model)
        {
            using (var db = new QLHANGHOA_PHUKIEN_WEBEntities())
            {
                var mergedItems = new Dictionary<string, Tuple<int, decimal>>(StringComparer.OrdinalIgnoreCase);

                if (model.MaBienSanPhams != null && model.SoLuongs != null && model.DonGias != null)
                {
                    var count = Math.Min(model.MaBienSanPhams.Count, Math.Min(model.SoLuongs.Count, model.DonGias.Count));
                    for (var i = 0; i < count; i++)
                    {
                        var variantId = (model.MaBienSanPhams[i] ?? string.Empty).Trim();
                        var qty = model.SoLuongs[i];
                        var price = model.DonGias[i];

                        if (string.IsNullOrWhiteSpace(variantId) || qty <= 0)
                        {
                            continue;
                        }

                        if (price < 0)
                        {
                            ModelState.AddModelError(string.Empty, "Đơn giá nhập không được nhỏ hơn 0.");
                            break;
                        }

                        if (mergedItems.ContainsKey(variantId))
                        {
                            var prev = mergedItems[variantId];
                            mergedItems[variantId] = Tuple.Create(prev.Item1 + qty, price);
                        }
                        else
                        {
                            mergedItems[variantId] = Tuple.Create(qty, price);
                        }
                    }
                }

                if (mergedItems.Count == 0)
                {
                    ModelState.AddModelError(string.Empty, "Vui lòng chọn ít nhất một biến thể sản phẩm với số lượng lớn hơn 0.");
                }

                var receiptId = string.IsNullOrWhiteSpace(model.MaPhieuNhap)
                    ? GenerateNextReceiptId(db)
                    : model.MaPhieuNhap.Trim().ToUpperInvariant();

                if (db.PHIEUNHAPs.Any(x => x.MaPhieuNhap == receiptId))
                {
                    ModelState.AddModelError("MaPhieuNhap", "Mã phiếu nhập đã tồn tại. Vui lòng chọn mã khác.");
                }

                if (!ModelState.IsValid)
                {
                    PopulateCreateOptions(db, model);
                    return View(model);
                }

                using (var tx = db.Database.BeginTransaction())
                {
                    try
                    {
                        var now = DateTime.Now;
                        int? currentAccountId;
                        string currentStaffId;
                        AdminStatusHelper.ResolveCurrentActor(db, User.Identity.Name, out currentAccountId, out currentStaffId);

                        var status = model.TrangThai == "CHO_XU_LY" ? "CHO_XU_LY" : "DA_NHAP";
                        decimal tongTien = 0m;
                        var details = new List<CHITIETPHIEUNHAP>();

                        foreach (var kvp in mergedItems)
                        {
                            var variant = db.BIENSANPHAMs.FirstOrDefault(x => x.MaBienSanPham == kvp.Key);
                            if (variant == null)
                            {
                                tx.Rollback();
                                ModelState.AddModelError(string.Empty, "Không tìm thấy biến thể sản phẩm: " + kvp.Key);
                                PopulateCreateOptions(db, model);
                                return View(model);
                            }

                            var qty = kvp.Value.Item1;
                            var price = kvp.Value.Item2;
                            tongTien += qty * price;

                            details.Add(new CHITIETPHIEUNHAP
                            {
                                MaPhieuNhap = receiptId,
                                MaBienSanPham = variant.MaBienSanPham,
                                SoLuong = qty,
                                DonGia = price
                            });
                        }

                        var receipt = new PHIEUNHAP
                        {
                            MaPhieuNhap = receiptId,
                            MaNCC = model.MaNCC.Trim(),
                            MaNV = currentStaffId,
                            NgayNhap = now,
                            TrangThai = status,
                            TongTien = tongTien,
                            GhiChu = string.IsNullOrWhiteSpace(model.GhiChu) ? null : model.GhiChu.Trim()
                        };

                        db.PHIEUNHAPs.Add(receipt);
                        foreach (var detail in details)
                        {
                            db.CHITIETPHIEUNHAPs.Add(detail);
                        }
                        db.SaveChanges();

                        // Nếu trạng thái là DA_NHAP => Cập nhật tồn kho & ghi GIAODICHKHO qua InventoryService
                        if (status == "DA_NHAP")
                        {
                            ApplyStockReceiptToInventory(db, tx.UnderlyingTransaction, receiptId, currentStaffId, details, now);
                        }

                        tx.Commit();

                        TempData["SuccessMessage"] = status == "DA_NHAP"
                            ? string.Format("Đã tạo phiếu nhập {0} và cộng tồn kho thành công.", receiptId)
                            : string.Format("Đã lưu phiếu nhập {0} ở trạng thái Chờ xử lý.", receiptId);

                        return RedirectToAction("Details", new { id = receiptId });
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        ModelState.AddModelError(string.Empty, "Lỗi khi tạo phiếu nhập: " + ex.Message);
                        PopulateCreateOptions(db, model);
                        return View(model);
                    }
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmReceipt(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index");
            }

            using (var db = new QLHANGHOA_PHUKIEN_WEBEntities())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var receipt = db.PHIEUNHAPs
                        .Include(x => x.CHITIETPHIEUNHAPs)
                        .FirstOrDefault(x => x.MaPhieuNhap == id);

                    if (receipt == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy phiếu nhập " + id;
                        return RedirectToAction("Index");
                    }

                    if (receipt.TrangThai != "CHO_XU_LY")
                    {
                        TempData["ErrorMessage"] = "Chỉ có thể xác nhận nhập kho cho phiếu đang ở trạng thái Chờ xử lý.";
                        return RedirectToAction("Details", new { id });
                    }

                    var now = DateTime.Now;
                    int? currentAccountId;
                    string currentStaffId;
                    AdminStatusHelper.ResolveCurrentActor(db, User.Identity.Name, out currentAccountId, out currentStaffId);

                    receipt.TrangThai = "DA_NHAP";
                    receipt.NgayNhap = now;
                    db.SaveChanges();

                    ApplyStockReceiptToInventory(
                        db,
                        tx.UnderlyingTransaction,
                        receipt.MaPhieuNhap,
                        currentStaffId ?? receipt.MaNV,
                        receipt.CHITIETPHIEUNHAPs.ToList(),
                        now);

                    tx.Commit();

                    TempData["SuccessMessage"] = string.Format(
                        "Đã xác nhận phiếu nhập {0} và cập nhật tồn kho thành công!",
                        receipt.MaPhieuNhap);

                    return RedirectToAction("Details", new { id = receipt.MaPhieuNhap });
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    TempData["ErrorMessage"] = "Lỗi khi xác nhận phiếu nhập: " + ex.Message;
                    return RedirectToAction("Details", new { id });
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelReceipt(string id, string ghiChu)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index");
            }

            using (var db = new QLHANGHOA_PHUKIEN_WEBEntities())
            {
                var receipt = db.PHIEUNHAPs.FirstOrDefault(x => x.MaPhieuNhap == id);
                if (receipt == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy phiếu nhập " + id;
                    return RedirectToAction("Index");
                }

                if (receipt.TrangThai != "CHO_XU_LY")
                {
                    TempData["ErrorMessage"] = "Chỉ có thể hủy phiếu nhập đang ở trạng thái Chờ xử lý.";
                    return RedirectToAction("Details", new { id });
                }

                receipt.TrangThai = "DA_HUY";
                if (!string.IsNullOrWhiteSpace(ghiChu))
                {
                    receipt.GhiChu = string.IsNullOrWhiteSpace(receipt.GhiChu)
                        ? "Hủy phiếu: " + ghiChu.Trim()
                        : receipt.GhiChu + " | Hủy phiếu: " + ghiChu.Trim();
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = string.Format("Đã hủy phiếu nhập {0}.", receipt.MaPhieuNhap);
                return RedirectToAction("Details", new { id = receipt.MaPhieuNhap });
            }
        }

        private static void ApplyStockReceiptToInventory(
            QLHANGHOA_PHUKIEN_WEBEntities db,
            System.Data.Common.DbTransaction transaction,
            string maPhieuNhap,
            string maNV,
            IList<CHITIETPHIEUNHAP> details,
            DateTime now)
        {
            var staffId = string.IsNullOrWhiteSpace(maNV)
                ? (db.NHANVIENs.Where(x => x.TrangThai).Select(x => x.MaNV).FirstOrDefault() ?? "NV001")
                : maNV;

            var inventoryService = new InventoryService();
            foreach (var item in details)
            {
                var change = new InventoryChange
                {
                    OperationId = Guid.NewGuid(),
                    VariantId = item.MaBienSanPham,
                    Type = "NHAP",
                    Quantity = item.SoLuong,
                    StaffId = staffId,
                    ReceiptId = maPhieuNhap,
                    Reason = "Cập nhật tồn kho theo phiếu nhập " + maPhieuNhap
                };
                inventoryService.Apply(transaction, change);

                var variant = db.BIENSANPHAMs.FirstOrDefault(x => x.MaBienSanPham == item.MaBienSanPham);
                if (variant != null && item.DonGia > 0)
                {
                    variant.GiaNhap = item.DonGia;
                }
            }
            db.SaveChanges();
        }

        private static void PopulateCreateOptions(QLHANGHOA_PHUKIEN_WEBEntities db, CreateStockReceiptViewModel model)
        {
            model.Suppliers = db.NHACUNGCAPs
                .Where(x => x.TrangThai)
                .OrderBy(x => x.TenNCC)
                .Select(x => new SupplierOptionViewModel
                {
                    MaNCC = x.MaNCC,
                    TenNCC = x.TenNCC
                })
                .ToList();

            var variants = db.BIENSANPHAMs
                .Include(x => x.SANPHAM)
                .Include(x => x.TONKHO)
                .Where(x => x.TrangThai && x.SANPHAM.TrangThai)
                .OrderBy(x => x.SANPHAM.TenSanPham)
                .ThenBy(x => x.SKU)
                .ToList();

            model.Variants = variants.Select(x => new VariantOptionViewModel
            {
                MaBienSanPham = x.MaBienSanPham,
                SKU = x.SKU,
                TenHienThi = string.Format("{0} ({1})", x.SANPHAM.TenSanPham, FormatVariantSpecs(x)),
                GiaNhap = x.GiaNhap,
                GiaBan = x.GiaBan,
                SoLuongTon = x.TONKHO != null ? x.TONKHO.SoLuongTon : 0
            }).ToList();
        }

        private static string FormatVariantSpecs(BIENSANPHAM variant)
        {
            if (variant == null)
            {
                return string.Empty;
            }

            var specs = new List<string>();
            if (!string.IsNullOrWhiteSpace(variant.MauSac))
            {
                specs.Add(variant.MauSac);
            }
            if (variant.RAMGB.HasValue)
            {
                specs.Add(variant.RAMGB.Value + "GB RAM");
            }
            if (variant.BoNhoGB.HasValue)
            {
                specs.Add(variant.BoNhoGB.Value + "GB ROM");
            }

            return specs.Count > 0 ? string.Join(" / ", specs) : variant.SKU;
        }

        private static string GenerateNextReceiptId(QLHANGHOA_PHUKIEN_WEBEntities db)
        {
            var existingIds = db.PHIEUNHAPs
                .Where(x => x.MaPhieuNhap.StartsWith("PN"))
                .Select(x => x.MaPhieuNhap)
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

            return string.Format("PN{0:0000}", maxNumber + 1);
        }
    }
}
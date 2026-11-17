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
    public class OrdersController : Controller
    {
        private const int PageSize = 10;

        public ActionResult Index(
            string search,
            string status,
            string paymentStatus,
            string paymentMethod,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1)
        {
            using (var db = new QLHANGHOA_PHUKIEN_WEBEntities())
            {
                var allOrders = db.DONHANGs.AsQueryable();

                var model = new OrderIndexViewModel
                {
                    Search = search,
                    Status = status,
                    PaymentStatus = paymentStatus,
                    PaymentMethod = paymentMethod,
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalOrders = allOrders.Count(),
                    PendingCount = allOrders.Count(x => x.TrangThaiDonHang == "CHO_XAC_NHAN"),
                    ConfirmedCount = allOrders.Count(x => x.TrangThaiDonHang == "DA_XAC_NHAN"),
                    ShippingCount = allOrders.Count(x => x.TrangThaiDonHang == "DANG_GIAO"),
                    DeliveredCount = allOrders.Count(x => x.TrangThaiDonHang == "DA_GIAO"),
                    CanceledCount = allOrders.Count(x => x.TrangThaiDonHang == "DA_HUY"),
                    TotalValidRevenue = allOrders
                        .Where(x => x.TrangThaiDonHang != "DA_HUY")
                        .Select(x => (decimal?)x.TongTien)
                        .Sum() ?? 0m
                };

                var query = db.DONHANGs
                    .Include(x => x.KHACHHANG)
                    .Include(x => x.NHANVIEN)
                    .Include(x => x.CHITIETDONHANGs)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var kw = search.Trim();
                    query = query.Where(x =>
                        x.MaDonHang.Contains(kw) ||
                        x.TenNguoiNhan.Contains(kw) ||
                        x.SoDienThoaiNhan.Contains(kw) ||
                        (x.KHACHHANG != null && x.KHACHHANG.TenKH.Contains(kw)));
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(x => x.TrangThaiDonHang == status);
                }

                if (!string.IsNullOrWhiteSpace(paymentStatus))
                {
                    query = query.Where(x => x.TrangThaiThanhToan == paymentStatus);
                }

                if (!string.IsNullOrWhiteSpace(paymentMethod))
                {
                    query = query.Where(x => x.PhuongThucThanhToan == paymentMethod);
                }

                if (fromDate.HasValue)
                {
                    var start = fromDate.Value.Date;
                    query = query.Where(x => x.NgayDat >= start);
                }

                if (toDate.HasValue)
                {
                    var endExclusive = toDate.Value.Date.AddDays(1);
                    query = query.Where(x => x.NgayDat < endExclusive);
                }

                var totalFiltered = query.Count();
                var totalPages = Math.Max(1, (int)Math.Ceiling(totalFiltered / (double)PageSize));
                page = Math.Max(1, Math.Min(page, totalPages));

                var orders = query
                    .OrderByDescending(x => x.NgayDat)
                    .ThenByDescending(x => x.MaDonHang)
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                model.CurrentPage = page;
                model.TotalPages = totalPages;
                model.Orders = orders.Select(x => new OrderListItemViewModel
                {
                    MaDonHang = x.MaDonHang,
                    MaKH = x.MaKH,
                    TenKhachHang = x.KHACHHANG != null ? x.KHACHHANG.TenKH : "Khách vãng lai",
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
                var order = db.DONHANGs
                    .Include(x => x.KHACHHANG)
                    .Include(x => x.NHANVIEN)
                    .Include(x => x.CHITIETDONHANGs.Select(ct => ct.BIENSANPHAM.TONKHO))
                    .Include(x => x.LICHSUTRANGTHAIDONHANGs.Select(ls => ls.TAIKHOAN))
                    .Include(x => x.GIAODICHKHOes.Select(gd => gd.BIENSANPHAM))
                    .Include(x => x.GIAODICHKHOes.Select(gd => gd.NHANVIEN))
                    .FirstOrDefault(x => x.MaDonHang == id);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng có mã: " + id;
                    return RedirectToAction("Index");
                }

                var items = order.CHITIETDONHANGs
                    .OrderBy(x => x.MaBienSanPham)
                    .Select(x => new OrderDetailItemViewModel
                    {
                        MaBienSanPham = x.MaBienSanPham,
                        SKU = x.BIENSANPHAM != null ? x.BIENSANPHAM.SKU : x.MaBienSanPham,
                        TenSanPham = x.TenSanPham,
                        MauSac = x.BIENSANPHAM != null ? x.BIENSANPHAM.MauSac : null,
                        RAMGB = x.BIENSANPHAM != null ? x.BIENSANPHAM.RAMGB : null,
                        BoNhoGB = x.BIENSANPHAM != null ? x.BIENSANPHAM.BoNhoGB : null,
                        HinhAnhUrl = x.BIENSANPHAM != null ? x.BIENSANPHAM.HinhAnhUrl : null,
                        DonGia = x.DonGia,
                        SoLuong = x.SoLuong,
                        GiamGia = x.GiamGia,
                        ThanhTien = x.ThanhTien ?? ((x.DonGia * x.SoLuong) - x.GiamGia),
                        SoLuongTonHienTai = x.BIENSANPHAM != null && x.BIENSANPHAM.TONKHO != null
                            ? x.BIENSANPHAM.TONKHO.SoLuongTon
                            : 0
                    })
                    .ToList();

                var model = new OrderDetailViewModel
                {
                    MaDonHang = order.MaDonHang,
                    MaKH = order.MaKH,
                    TenKhachHang = order.KHACHHANG != null ? order.KHACHHANG.TenKH : "Khách vãng lai",
                    EmailKhachHang = order.KHACHHANG != null ? order.KHACHHANG.Email : null,
                    MaNVXuLy = order.MaNVXuLy,
                    TenNVXuLy = order.NHANVIEN != null ? order.NHANVIEN.TenNV : "Chưa có nhân viên xử lý",
                    NgayDat = order.NgayDat,
                    NgayCapNhat = order.NgayCapNhat,
                    TrangThaiDonHang = order.TrangThaiDonHang,
                    PhuongThucThanhToan = order.PhuongThucThanhToan,
                    TrangThaiThanhToan = order.TrangThaiThanhToan,
                    TenNguoiNhan = order.TenNguoiNhan,
                    SoDienThoaiNhan = order.SoDienThoaiNhan,
                    DiaChiGiaoHang = order.DiaChiGiaoHang,
                    TamTinh = order.TamTinh,
                    PhiVanChuyen = order.PhiVanChuyen,
                    GiamGia = order.GiamGia,
                    TongTien = order.TongTien,
                    GhiChu = order.GhiChu,
                    LyDoHuy = order.LyDoHuy,
                    CanConfirm = order.TrangThaiDonHang == "CHO_XAC_NHAN",
                    HasEnoughStockForAllItems = items.All(i => i.SoLuongTonHienTai >= i.SoLuong),
                    CanShip = order.TrangThaiDonHang == "DA_XAC_NHAN",
                    CanDeliver = order.TrangThaiDonHang == "DANG_GIAO",
                    CanCancel = order.TrangThaiDonHang == "CHO_XAC_NHAN"
                        || order.TrangThaiDonHang == "DA_XAC_NHAN"
                        || order.TrangThaiDonHang == "DANG_GIAO",
                    Items = items,
                    StatusHistories = order.LICHSUTRANGTHAIDONHANGs
                        .OrderByDescending(x => x.ThoiGian)
                        .ThenByDescending(x => x.MaLichSu)
                        .Select(x => new OrderStatusHistoryItemViewModel
                        {
                            MaLichSu = x.MaLichSu,
                            TrangThaiCu = x.TrangThaiCu,
                            TrangThaiMoi = x.TrangThaiMoi,
                            NguoiThucHien = x.TAIKHOAN != null ? x.TAIKHOAN.TenDangNhap : "Hệ thống",
                            ThoiGian = x.ThoiGian,
                            GhiChu = x.GhiChu
                        })
                        .ToList(),
                    InventoryLogs = order.GIAODICHKHOes
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmOrder(string id, string ghiChu)
        {
            return ProcessStatusTransition(
                id,
                "DA_XAC_NHAN",
                null,
                string.IsNullOrWhiteSpace(ghiChu)
                    ? "Xác nhận đơn hàng và trừ tồn kho"
                    : ghiChu.Trim(),
                null);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelOrder(string id, string lyDoHuy)
        {
            if (string.IsNullOrWhiteSpace(lyDoHuy))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do hủy đơn hàng.";
                return RedirectToAction("Details", new { id });
            }

            return ProcessStatusTransition(
                id,
                "DA_HUY",
                null,
                "Hủy đơn hàng: " + lyDoHuy.Trim(),
                lyDoHuy.Trim());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(string id, string trangThaiMoi, string trangThaiThanhToan, string ghiChu, string lyDoHuy)
        {
            return ProcessStatusTransition(id, trangThaiMoi, trangThaiThanhToan, ghiChu, lyDoHuy);
        }

        private ActionResult ProcessStatusTransition(
            string id,
            string trangThaiMoi,
            string trangThaiThanhToan,
            string ghiChu,
            string lyDoHuy)
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
                    var order = db.DONHANGs
                        .Include(x => x.CHITIETDONHANGs.Select(ct => ct.BIENSANPHAM))
                        .FirstOrDefault(x => x.MaDonHang == id);

                    if (order == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy đơn hàng " + id;
                        return RedirectToAction("Index");
                    }

                    var oldStatus = order.TrangThaiDonHang;
                    var targetStatus = string.IsNullOrWhiteSpace(trangThaiMoi) ? oldStatus : trangThaiMoi.Trim().ToUpperInvariant();

                    var allowed = AdminStatusHelper.GetAllowedNextOrderStatuses(oldStatus);
                    if (!allowed.Contains(targetStatus))
                    {
                        TempData["ErrorMessage"] = string.Format(
                            "Không thể chuyển trạng thái đơn hàng từ '{0}' sang '{1}'.",
                            AdminStatusHelper.GetOrderStatusLabel(oldStatus),
                            AdminStatusHelper.GetOrderStatusLabel(targetStatus));
                        return RedirectToAction("Details", new { id });
                    }

                    int? currentAccountId;
                    string currentStaffId;
                    AdminStatusHelper.ResolveCurrentActor(db, User.Identity.Name, out currentAccountId, out currentStaffId);
                    var now = DateTime.Now;

                    // Trường hợp 1: Xác nhận đơn hàng (CHO_XAC_NHAN -> DA_XAC_NHAN) => Kiểm tra & Trừ tồn kho
                    if (oldStatus == "CHO_XAC_NHAN" && targetStatus == "DA_XAC_NHAN")
                    {
                        foreach (var item in order.CHITIETDONHANGs)
                        {
                            var stock = db.TONKHOes.FirstOrDefault(x => x.MaBienSanPham == item.MaBienSanPham);
                            var currentStock = stock != null ? stock.SoLuongTon : 0;
                            if (stock == null || currentStock < item.SoLuong)
                            {
                                tx.Rollback();
                                TempData["ErrorMessage"] = string.Format(
                                    "Không thể xác nhận đơn hàng! Sản phẩm '{0}' ({1}) chỉ còn {2} trong kho, nhưng đơn hàng cần {3}.",
                                    item.TenSanPham,
                                    item.MaBienSanPham,
                                    currentStock,
                                    item.SoLuong);
                                return RedirectToAction("Details", new { id });
                            }
                        }

                        foreach (var item in order.CHITIETDONHANGs)
                        {
                            var stock = db.TONKHOes.First(x => x.MaBienSanPham == item.MaBienSanPham);
                            var before = stock.SoLuongTon;
                            stock.SoLuongTon -= item.SoLuong;
                            if (stock.SoLuongDangDat >= item.SoLuong)
                            {
                                stock.SoLuongDangDat -= item.SoLuong;
                            }
                            stock.NgayCapNhat = now;

                            db.GIAODICHKHOes.Add(new GIAODICHKHO
                            {
                                MaBienSanPham = item.MaBienSanPham,
                                LoaiGiaoDich = "BAN",
                                SoLuong = item.SoLuong,
                                SoLuongTruoc = before,
                                SoLuongSau = stock.SoLuongTon,
                                MaDonHang = order.MaDonHang,
                                MaNV = currentStaffId,
                                NgayGiaoDich = now,
                                GhiChu = "Trừ tồn kho khi xác nhận đơn hàng " + order.MaDonHang
                            });
                        }
                    }

                    // Trường hợp 2: Hủy đơn hàng từ trạng thái đã trừ tồn kho (DA_XAC_NHAN hoặc DANG_GIAO -> DA_HUY) => Hoàn lại tồn kho
                    if (targetStatus == "DA_HUY" && oldStatus != "DA_HUY")
                    {
                        var reason = !string.IsNullOrWhiteSpace(lyDoHuy)
                            ? lyDoHuy.Trim()
                            : (!string.IsNullOrWhiteSpace(ghiChu) ? ghiChu.Trim() : "Admin hủy đơn hàng");

                        order.LyDoHuy = reason;

                        if (oldStatus == "DA_XAC_NHAN" || oldStatus == "DANG_GIAO")
                        {
                            foreach (var item in order.CHITIETDONHANGs)
                            {
                                var stock = db.TONKHOes.FirstOrDefault(x => x.MaBienSanPham == item.MaBienSanPham);
                                if (stock == null)
                                {
                                    stock = new TONKHO
                                    {
                                        MaBienSanPham = item.MaBienSanPham,
                                        SoLuongTon = 0,
                                        SoLuongDangDat = 0,
                                        NgayCapNhat = now
                                    };
                                    db.TONKHOes.Add(stock);
                                }

                                var before = stock.SoLuongTon;
                                stock.SoLuongTon += item.SoLuong;
                                stock.NgayCapNhat = now;

                                db.GIAODICHKHOes.Add(new GIAODICHKHO
                                {
                                    MaBienSanPham = item.MaBienSanPham,
                                    LoaiGiaoDich = "TRA_HANG",
                                    SoLuong = item.SoLuong,
                                    SoLuongTruoc = before,
                                    SoLuongSau = stock.SoLuongTon,
                                    MaDonHang = order.MaDonHang,
                                    MaNV = currentStaffId,
                                    NgayGiaoDich = now,
                                    GhiChu = "Hoàn trả tồn kho do hủy đơn hàng " + order.MaDonHang
                                });
                            }
                        }

                        if (order.TrangThaiThanhToan == "DA_THANH_TOAN")
                        {
                            order.TrangThaiThanhToan = "HOAN_TIEN";
                        }
                    }

                    // Trường hợp 3: Đã giao thành công => Tự động cập nhật Đã thanh toán
                    if (targetStatus == "DA_GIAO" && order.TrangThaiThanhToan == "CHUA_THANH_TOAN")
                    {
                        order.TrangThaiThanhToan = "DA_THANH_TOAN";
                    }

                    // Cập nhật trạng thái thanh toán nếu được chọn rõ ràng
                    if (!string.IsNullOrWhiteSpace(trangThaiThanhToan)
                        && AdminStatusHelper.PaymentStatuses.Contains(trangThaiThanhToan.Trim().ToUpperInvariant()))
                    {
                        var newPaymentStatus = trangThaiThanhToan.Trim().ToUpperInvariant();
                        if (targetStatus != "DA_GIAO" || newPaymentStatus != "CHUA_THANH_TOAN")
                        {
                            order.TrangThaiThanhToan = newPaymentStatus;
                        }
                    }

                    var statusChanged = oldStatus != targetStatus;
                    order.TrangThaiDonHang = targetStatus;
                    order.NgayCapNhat = now;
                    if (!string.IsNullOrWhiteSpace(currentStaffId))
                    {
                        order.MaNVXuLy = currentStaffId;
                    }

                    if (statusChanged || !string.IsNullOrWhiteSpace(ghiChu))
                    {
                        var historyNote = !string.IsNullOrWhiteSpace(ghiChu)
                            ? ghiChu.Trim()
                            : "Cập nhật trạng thái sang " + AdminStatusHelper.GetOrderStatusLabel(targetStatus);

                        db.LICHSUTRANGTHAIDONHANGs.Add(new LICHSUTRANGTHAIDONHANG
                        {
                            MaDonHang = order.MaDonHang,
                            TrangThaiCu = oldStatus,
                            TrangThaiMoi = targetStatus,
                            MaTaiKhoan = currentAccountId,
                            ThoiGian = now,
                            GhiChu = historyNote
                        });
                    }

                    db.SaveChanges();
                    tx.Commit();

                    TempData["SuccessMessage"] = string.Format(
                        "Đã cập nhật đơn hàng {0} sang trạng thái '{1}' thành công.",
                        order.MaDonHang,
                        AdminStatusHelper.GetOrderStatusLabel(targetStatus));

                    return RedirectToAction("Details", new { id = order.MaDonHang });
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    TempData["ErrorMessage"] = "Lỗi khi cập nhật đơn hàng: " + ex.Message;
                    return RedirectToAction("Details", new { id });
                }
            }
        }

        public ActionResult Create()
        {
            using (var db = new QLHANGHOA_PHUKIEN_WEBEntities())
            {
                var model = new CreateOrderViewModel
                {
                    PhuongThucThanhToan = "COD",
                    PhiVanChuyen = 0m,
                    GiamGia = 0m,
                    XacNhanNgay = false
                };

                PopulateCreateOrderOptions(db, model);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateOrderViewModel model)
        {
            using (var db = new QLHANGHOA_PHUKIEN_WEBEntities())
            {
                var lineItems = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                if (model.MaBienSanPhams != null && model.SoLuongs != null)
                {
                    var count = Math.Min(model.MaBienSanPhams.Count, model.SoLuongs.Count);
                    for (var i = 0; i < count; i++)
                    {
                        var variantId = (model.MaBienSanPhams[i] ?? string.Empty).Trim();
                        var qty = model.SoLuongs[i];
                        if (string.IsNullOrWhiteSpace(variantId) || qty <= 0)
                        {
                            continue;
                        }

                        if (lineItems.ContainsKey(variantId))
                        {
                            lineItems[variantId] += qty;
                        }
                        else
                        {
                            lineItems[variantId] = qty;
                        }
                    }
                }

                if (lineItems.Count == 0)
                {
                    ModelState.AddModelError(string.Empty, "Vui lòng chọn ít nhất một sản phẩm với số lượng lớn hơn 0.");
                }

                if (!ModelState.IsValid)
                {
                    PopulateCreateOrderOptions(db, model);
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

                        var nextOrderId = GenerateNextOrderId(db);
                        decimal tamTinh = 0m;
                        var orderDetails = new List<CHITIETDONHANG>();

                        foreach (var kvp in lineItems)
                        {
                            var variant = db.BIENSANPHAMs
                                .Include(x => x.SANPHAM)
                                .Include(x => x.TONKHO)
                                .FirstOrDefault(x => x.MaBienSanPham == kvp.Key && x.TrangThai);

                            if (variant == null)
                            {
                                tx.Rollback();
                                ModelState.AddModelError(string.Empty, "Không tìm thấy biến thể sản phẩm: " + kvp.Key);
                                PopulateCreateOrderOptions(db, model);
                                return View(model);
                            }

                            var currentStock = variant.TONKHO != null ? variant.TONKHO.SoLuongTon : 0;
                            if (currentStock < kvp.Value)
                            {
                                tx.Rollback();
                                ModelState.AddModelError(
                                    string.Empty,
                                    string.Format("Sản phẩm {0} ({1}) chỉ còn {2} trong kho (yêu cầu {3}).",
                                        variant.SANPHAM.TenSanPham, variant.SKU, currentStock, kvp.Value));
                                PopulateCreateOrderOptions(db, model);
                                return View(model);
                            }

                            var fullName = BuildVariantDisplayName(variant);
                            var subTotal = variant.GiaBan * kvp.Value;
                            tamTinh += subTotal;

                            orderDetails.Add(new CHITIETDONHANG
                            {
                                MaDonHang = nextOrderId,
                                MaBienSanPham = variant.MaBienSanPham,
                                TenSanPham = fullName,
                                DonGia = variant.GiaBan,
                                SoLuong = kvp.Value,
                                GiamGia = 0m
                            });
                        }

                        var phiVC = Math.Max(0m, model.PhiVanChuyen);
                        var giamGia = Math.Max(0m, Math.Min(tamTinh, model.GiamGia));
                        var tongTien = Math.Max(0m, tamTinh + phiVC - giamGia);
                        var initialStatus = model.XacNhanNgay ? "DA_XAC_NHAN" : "CHO_XAC_NHAN";
                        var paymentMethod = AdminStatusHelper.PaymentMethods.Contains(model.PhuongThucThanhToan)
                            ? model.PhuongThucThanhToan
                            : "COD";

                        var order = new DONHANG
                        {
                            MaDonHang = nextOrderId,
                            MaKH = string.IsNullOrWhiteSpace(model.MaKH) ? null : model.MaKH.Trim(),
                            MaNVXuLy = currentStaffId,
                            NgayDat = now,
                            NgayCapNhat = now,
                            TrangThaiDonHang = initialStatus,
                            PhuongThucThanhToan = paymentMethod,
                            TrangThaiThanhToan = "CHUA_THANH_TOAN",
                            TenNguoiNhan = model.TenNguoiNhan.Trim(),
                            SoDienThoaiNhan = model.SoDienThoaiNhan.Trim(),
                            DiaChiGiaoHang = model.DiaChiGiaoHang.Trim(),
                            TamTinh = tamTinh,
                            PhiVanChuyen = phiVC,
                            GiamGia = giamGia,
                            TongTien = tongTien,
                            GhiChu = string.IsNullOrWhiteSpace(model.GhiChu) ? null : model.GhiChu.Trim()
                        };

                        db.DONHANGs.Add(order);
                        foreach (var detail in orderDetails)
                        {
                            db.CHITIETDONHANGs.Add(detail);
                        }

                        db.LICHSUTRANGTHAIDONHANGs.Add(new LICHSUTRANGTHAIDONHANG
                        {
                            MaDonHang = nextOrderId,
                            TrangThaiCu = null,
                            TrangThaiMoi = "CHO_XAC_NHAN",
                            MaTaiKhoan = currentAccountId,
                            ThoiGian = now,
                            GhiChu = "Khởi tạo đơn hàng tại trang Quản trị"
                        });

                        if (model.XacNhanNgay)
                        {
                            foreach (var detail in orderDetails)
                            {
                                var stock = db.TONKHOes.First(x => x.MaBienSanPham == detail.MaBienSanPham);
                                var before = stock.SoLuongTon;
                                stock.SoLuongTon -= detail.SoLuong;
                                stock.NgayCapNhat = now;

                                db.GIAODICHKHOes.Add(new GIAODICHKHO
                                {
                                    MaBienSanPham = detail.MaBienSanPham,
                                    LoaiGiaoDich = "BAN",
                                    SoLuong = detail.SoLuong,
                                    SoLuongTruoc = before,
                                    SoLuongSau = stock.SoLuongTon,
                                    MaDonHang = nextOrderId,
                                    MaNV = currentStaffId,
                                    NgayGiaoDich = now,
                                    GhiChu = "Trừ tồn kho khi tạo và xác nhận ngay đơn hàng " + nextOrderId
                                });
                            }

                            db.LICHSUTRANGTHAIDONHANGs.Add(new LICHSUTRANGTHAIDONHANG
                            {
                                MaDonHang = nextOrderId,
                                TrangThaiCu = "CHO_XAC_NHAN",
                                TrangThaiMoi = "DA_XAC_NHAN",
                                MaTaiKhoan = currentAccountId,
                                ThoiGian = now,
                                GhiChu = "Xác nhận ngay và trừ tồn kho"
                            });
                        }

                        db.SaveChanges();
                        tx.Commit();

                        TempData["SuccessMessage"] = string.Format(
                            "Đã tạo đơn hàng {0} ({1}) thành công.",
                            nextOrderId,
                            AdminStatusHelper.GetOrderStatusLabel(initialStatus));

                        return RedirectToAction("Details", new { id = nextOrderId });
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        ModelState.AddModelError(string.Empty, "Không thể tạo đơn hàng: " + ex.Message);
                        PopulateCreateOrderOptions(db, model);
                        return View(model);
                    }
                }
            }
        }

        private static void PopulateCreateOrderOptions(QLHANGHOA_PHUKIEN_WEBEntities db, CreateOrderViewModel model)
        {
            model.AvailableCustomers = db.KHACHHANGs
                .Where(x => x.TrangThai)
                .OrderBy(x => x.MaKH)
                .Select(x => new CustomerOptionViewModel
                {
                    MaKH = x.MaKH,
                    TenKH = x.TenKH,
                    DienThoai = x.DienThoai,
                    DiaChi = x.DiaChi
                })
                .ToList();

            var variants = db.BIENSANPHAMs
                .Include(x => x.SANPHAM)
                .Include(x => x.TONKHO)
                .Where(x => x.TrangThai && x.SANPHAM.TrangThai)
                .OrderBy(x => x.SANPHAM.TenSanPham)
                .ThenBy(x => x.SKU)
                .ToList();

            model.AvailableVariants = variants
                .Select(x => new VariantOptionViewModel
                {
                    MaBienSanPham = x.MaBienSanPham,
                    SKU = x.SKU,
                    TenHienThi = BuildVariantDisplayName(x),
                    GiaNhap = x.GiaNhap,
                    GiaBan = x.GiaBan,
                    SoLuongTon = x.TONKHO != null ? x.TONKHO.SoLuongTon : 0
                })
                .ToList();
        }

        private static string BuildVariantDisplayName(BIENSANPHAM variant)
        {
            var parts = new List<string> { variant.SANPHAM != null ? variant.SANPHAM.TenSanPham : variant.MaSanPham };
            if (variant.BoNhoGB.HasValue)
            {
                parts.Add(variant.BoNhoGB.Value + "GB");
            }
            if (!string.IsNullOrWhiteSpace(variant.MauSac))
            {
                parts.Add(variant.MauSac);
            }
            return string.Join(" - ", parts);
        }

        private static string GenerateNextOrderId(QLHANGHOA_PHUKIEN_WEBEntities db)
        {
            var existingIds = db.DONHANGs
                .Where(x => x.MaDonHang.StartsWith("DH"))
                .Select(x => x.MaDonHang)
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

            return string.Format("DH{0:0000}", maxNumber + 1);
        }
    }
}

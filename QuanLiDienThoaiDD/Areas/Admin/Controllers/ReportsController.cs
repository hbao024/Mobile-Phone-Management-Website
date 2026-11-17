using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using QuanLiDienThoaiDD.Areas.Admin.ViewModels;
using QuanLiDienThoaiDD.Models;

namespace QuanLiDienThoaiDD.Areas.Admin.Controllers
{
    [Authorize(Roles = "ADMIN,NHANVIEN")]
    public class ReportsController : Controller
    {
        public ActionResult Index(DateTime? fromDate, DateTime? toDate, string period = "all")
        {
            var model = BuildReportModel(fromDate, toDate, period);
            return View(model);
        }

        public ActionResult ExportRevenueCsv(DateTime? fromDate, DateTime? toDate, string period = "all")
        {
            var model = BuildReportModel(fromDate, toDate, period);
            var sb = new StringBuilder();

            sb.AppendLine("BAO CAO THONG KE DOANH THU VA SAN PHAM BAN RA - MOBILEHUB");
            sb.AppendLine(string.Format("Ky bao cao:,\"{0}\"", model.PeriodDescription));
            sb.AppendLine(string.Format("Tong doanh thu hop le:,{0}", model.TotalValidRevenue));
            sb.AppendLine(string.Format("Doanh thu da giao:,{0}", model.DeliveredRevenue));
            sb.AppendLine(string.Format("Loi nhuan gop uoc tinh:,{0}", model.EstimatedGrossProfit));
            sb.AppendLine(string.Format("Tong so don hang:,{0}", model.TotalOrders));
            sb.AppendLine(string.Format("Tong so san pham ban ra:,{0}", model.TotalUnitsSold));
            sb.AppendLine();

            sb.AppendLine("1. THONG KE DOANH THU THEO NGAY");
            sb.AppendLine("Ngay,So don hang,So don da giao,So don huy,So luong SP ban,Tam tinh,Giam gia,Phi van chuyen,Doanh thu hop le,Doanh thu da giao,Gia von uoc tinh,Loi nhuan uoc tinh");
            foreach (var row in model.RevenueByDates)
            {
                sb.AppendLine(string.Format(
                    "{0:dd/MM/yyyy},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}",
                    row.Ngay,
                    row.SoDonHang,
                    row.SoDonDaGiao,
                    row.SoDonHuy,
                    row.SoSanPhamBan,
                    row.TamTinh,
                    row.GiamGia,
                    row.PhiVanChuyen,
                    row.DoanhThuHopLe,
                    row.DoanhThuDaGiao,
                    row.GiaVonUocTinh,
                    row.LoiNhuanUocTinh));
            }

            sb.AppendLine();
            sb.AppendLine("2. THONG KE SO LUONG SAN PHAM BAN RA");
            sb.AppendLine("Ma bien the,SKU,Ten san pham,Thuong hieu,Danh muc,So don hang,So luong ban ra,So luong da giao,Doanh thu,Gia von uoc tinh,Loi nhuan uoc tinh,Ton kho hien tai");
            foreach (var item in model.ProductSales)
            {
                sb.AppendLine(string.Format(
                    "\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",{5},{6},{7},{8},{9},{10},{11}",
                    item.MaBienSanPham,
                    item.SKU,
                    (item.TenSanPham ?? string.Empty).Replace("\"", "\"\""),
                    (item.TenThuongHieu ?? string.Empty).Replace("\"", "\"\""),
                    (item.TenLoai ?? string.Empty).Replace("\"", "\"\""),
                    item.SoDonCoSanPham,
                    item.SoLuongBanRa,
                    item.SoLuongDaGiao,
                    item.DoanhThuBanRa,
                    item.GiaVonUocTinh,
                    item.LoiNhuanUocTinh,
                    item.SoLuongTonHienTai));
            }

            var preamble = Encoding.UTF8.GetPreamble();
            var body = Encoding.UTF8.GetBytes(sb.ToString());
            var bytes = new byte[preamble.Length + body.Length];
            Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
            Buffer.BlockCopy(body, 0, bytes, preamble.Length, body.Length);

            var fileName = string.Format("BaoCao_MobileHub_{0:yyyyMMdd_HHmm}.csv", DateTime.Now);
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        private static ReportDashboardViewModel BuildReportModel(DateTime? fromDate, DateTime? toDate, string period)
        {
            var today = DateTime.Today;
            var normalizedPeriod = (period ?? "all").Trim().ToLowerInvariant();

            if (!fromDate.HasValue && !toDate.HasValue)
            {
                switch (normalizedPeriod)
                {
                    case "today":
                        fromDate = today;
                        toDate = today;
                        break;
                    case "7days":
                        fromDate = today.AddDays(-6);
                        toDate = today;
                        break;
                    case "30days":
                        fromDate = today.AddDays(-29);
                        toDate = today;
                        break;
                    case "month":
                        fromDate = new DateTime(today.Year, today.Month, 1);
                        toDate = today;
                        break;
                    case "year":
                        fromDate = new DateTime(today.Year, 1, 1);
                        toDate = today;
                        break;
                }
            }
            else
            {
                normalizedPeriod = "custom";
            }

            using (var db = new QLHANGHOA_PHUKIEN_WEBEntities())
            {
                var ordersQuery = db.DONHANGs
                    .Include(x => x.CHITIETDONHANGs.Select(ct => ct.BIENSANPHAM.SANPHAM.THUONGHIEU))
                    .Include(x => x.CHITIETDONHANGs.Select(ct => ct.BIENSANPHAM.SANPHAM.LOAISANPHAM))
                    .Include(x => x.CHITIETDONHANGs.Select(ct => ct.BIENSANPHAM.TONKHO))
                    .AsQueryable();

                var receiptsQuery = db.PHIEUNHAPs
                    .Include(x => x.CHITIETPHIEUNHAPs)
                    .Where(x => x.TrangThai == "DA_NHAP")
                    .AsQueryable();

                if (fromDate.HasValue)
                {
                    var start = fromDate.Value.Date;
                    ordersQuery = ordersQuery.Where(x => x.NgayDat >= start);
                    receiptsQuery = receiptsQuery.Where(x => x.NgayNhap >= start);
                }

                if (toDate.HasValue)
                {
                    var endExclusive = toDate.Value.Date.AddDays(1);
                    ordersQuery = ordersQuery.Where(x => x.NgayDat < endExclusive);
                    receiptsQuery = receiptsQuery.Where(x => x.NgayNhap < endExclusive);
                }

                var orders = ordersQuery.OrderByDescending(x => x.NgayDat).ToList();
                var receipts = receiptsQuery.OrderByDescending(x => x.NgayNhap).ToList();

                var validOrders = orders.Where(x => x.TrangThaiDonHang != "DA_HUY").ToList();
                var deliveredOrders = orders.Where(x => x.TrangThaiDonHang == "DA_GIAO").ToList();
                var processingOrders = orders.Where(x =>
                    x.TrangThaiDonHang == "CHO_XAC_NHAN" ||
                    x.TrangThaiDonHang == "DA_XAC_NHAN" ||
                    x.TrangThaiDonHang == "DANG_GIAO").ToList();
                var canceledOrders = orders.Where(x => x.TrangThaiDonHang == "DA_HUY").ToList();

                var totalValidRevenue = validOrders.Sum(x => x.TongTien);
                var deliveredRevenue = deliveredOrders.Sum(x => x.TongTien);
                var processingRevenue = processingOrders.Sum(x => x.TongTien);
                var canceledRevenue = canceledOrders.Sum(x => x.TongTien);

                decimal totalCogs = 0m;
                int totalUnitsSold = 0;
                foreach (var order in validOrders)
                {
                    foreach (var ct in order.CHITIETDONHANGs)
                    {
                        totalUnitsSold += ct.SoLuong;
                        var unitCost = ct.BIENSANPHAM != null ? ct.BIENSANPHAM.GiaNhap : 0m;
                        totalCogs += unitCost * ct.SoLuong;
                    }
                }

                var totalImportCost = receipts.Sum(x => x.TongTien);
                var totalUnitsImported = receipts
                    .SelectMany(x => x.CHITIETPHIEUNHAPs)
                    .Sum(x => (int?)x.SoLuong) ?? 0;

                // 1. Doanh thu theo ngày
                var revenueByDates = orders
                    .GroupBy(x => x.NgayDat.Date)
                    .OrderByDescending(g => g.Key)
                    .Select(g =>
                    {
                        var validInDay = g.Where(o => o.TrangThaiDonHang != "DA_HUY").ToList();
                        var deliveredInDay = g.Where(o => o.TrangThaiDonHang == "DA_GIAO").ToList();
                        var revValid = validInDay.Sum(o => o.TongTien);
                        var cogsDay = validInDay
                            .SelectMany(o => o.CHITIETDONHANGs)
                            .Sum(ct => (ct.BIENSANPHAM != null ? ct.BIENSANPHAM.GiaNhap : 0m) * ct.SoLuong);

                        return new RevenueByDateItemViewModel
                        {
                            Ngay = g.Key,
                            SoDonHang = g.Count(),
                            SoDonDaGiao = deliveredInDay.Count,
                            SoDonHuy = g.Count(o => o.TrangThaiDonHang == "DA_HUY"),
                            SoSanPhamBan = validInDay.SelectMany(o => o.CHITIETDONHANGs).Sum(ct => ct.SoLuong),
                            TamTinh = validInDay.Sum(o => o.TamTinh),
                            GiamGia = validInDay.Sum(o => o.GiamGia),
                            PhiVanChuyen = validInDay.Sum(o => o.PhiVanChuyen),
                            DoanhThuHopLe = revValid,
                            DoanhThuDaGiao = deliveredInDay.Sum(o => o.TongTien),
                            GiaVonUocTinh = cogsDay,
                            LoiNhuanUocTinh = revValid - cogsDay
                        };
                    })
                    .ToList();

                // 2. Doanh thu theo tháng
                var allMonthKeys = orders
                    .Select(x => new { Nam = x.NgayDat.Year, Thang = x.NgayDat.Month })
                    .Union(receipts.Select(x => new { Nam = x.NgayNhap.Year, Thang = x.NgayNhap.Month }))
                    .Distinct()
                    .OrderByDescending(x => x.Nam)
                    .ThenByDescending(x => x.Thang)
                    .ToList();

                var revenueByMonths = allMonthKeys.Select(m =>
                {
                    var monthOrders = orders.Where(x => x.NgayDat.Year == m.Nam && x.NgayDat.Month == m.Thang).ToList();
                    var monthValid = monthOrders.Where(x => x.TrangThaiDonHang != "DA_HUY").ToList();
                    var monthDelivered = monthOrders.Where(x => x.TrangThaiDonHang == "DA_GIAO").ToList();
                    var monthReceipts = receipts.Where(x => x.NgayNhap.Year == m.Nam && x.NgayNhap.Month == m.Thang).ToList();

                    var revValid = monthValid.Sum(o => o.TongTien);
                    var cogsMonth = monthValid
                        .SelectMany(o => o.CHITIETDONHANGs)
                        .Sum(ct => (ct.BIENSANPHAM != null ? ct.BIENSANPHAM.GiaNhap : 0m) * ct.SoLuong);

                    return new RevenueByMonthItemViewModel
                    {
                        Nam = m.Nam,
                        Thang = m.Thang,
                        SoDonHang = monthOrders.Count,
                        SoSanPhamBan = monthValid.SelectMany(o => o.CHITIETDONHANGs).Sum(ct => ct.SoLuong),
                        DoanhThuHopLe = revValid,
                        DoanhThuDaGiao = monthDelivered.Sum(o => o.TongTien),
                        ChiPhiNhapKho = monthReceipts.Sum(r => r.TongTien),
                        LoiNhuanGop = revValid - cogsMonth
                    };
                }).ToList();

                // 3. Phân bổ theo trạng thái đơn hàng
                var totalOrdersCount = orders.Count;
                var statusBreakdowns = AdminStatusHelper.OrderStatuses.Select(st =>
                {
                    var matched = orders.Where(o => o.TrangThaiDonHang == st).ToList();
                    return new OrderStatusBreakdownViewModel
                    {
                        TrangThai = st,
                        TenTrangThai = AdminStatusHelper.GetOrderStatusLabel(st),
                        SoDon = matched.Count,
                        TongGiaTri = matched.Sum(o => o.TongTien),
                        TyLePhanTram = totalOrdersCount > 0
                            ? Math.Round(matched.Count * 100m / totalOrdersCount, 1)
                            : 0m
                    };
                }).ToList();

                // 4. Phân bổ theo phương thức thanh toán
                var paymentBreakdowns = AdminStatusHelper.PaymentMethods.Select(pm =>
                {
                    var matched = validOrders.Where(o => o.PhuongThucThanhToan == pm).ToList();
                    var rev = matched.Sum(o => o.TongTien);
                    return new PaymentMethodBreakdownViewModel
                    {
                        PhuongThuc = pm,
                        TenPhuongThuc = AdminStatusHelper.GetPaymentMethodLabel(pm),
                        SoDon = matched.Count,
                        DoanhThu = rev,
                        TyLePhanTram = totalValidRevenue > 0
                            ? Math.Round(rev * 100m / totalValidRevenue, 1)
                            : 0m
                    };
                }).ToList();

                // 5. Thống kê số lượng sản phẩm bán ra (bao gồm cả sản phẩm chưa bán được để theo dõi toàn diện)
                var allVariants = db.BIENSANPHAMs
                    .Include(x => x.SANPHAM.THUONGHIEU)
                    .Include(x => x.SANPHAM.LOAISANPHAM)
                    .Include(x => x.TONKHO)
                    .ToList();

                var validOrderLines = validOrders
                    .SelectMany(o => o.CHITIETDONHANGs.Select(ct => new { Order = o, Detail = ct }))
                    .ToList();

                var totalLineRevenue = validOrderLines.Sum(x => x.Detail.ThanhTien ?? ((x.Detail.DonGia * x.Detail.SoLuong) - x.Detail.GiamGia));

                var productSales = allVariants
                    .Select(v =>
                    {
                        var lines = validOrderLines.Where(x => x.Detail.MaBienSanPham == v.MaBienSanPham).ToList();
                        var qtySold = lines.Sum(x => x.Detail.SoLuong);
                        var qtyDelivered = lines
                            .Where(x => x.Order.TrangThaiDonHang == "DA_GIAO")
                            .Sum(x => x.Detail.SoLuong);
                        var rev = lines.Sum(x => x.Detail.ThanhTien ?? ((x.Detail.DonGia * x.Detail.SoLuong) - x.Detail.GiamGia));
                        var cogs = qtySold * v.GiaNhap;

                        return new ProductSalesStatItemViewModel
                        {
                            MaBienSanPham = v.MaBienSanPham,
                            SKU = v.SKU,
                            MaSanPham = v.MaSanPham,
                            TenSanPham = v.SANPHAM != null ? v.SANPHAM.TenSanPham : v.MaSanPham,
                            TenThuongHieu = v.SANPHAM != null && v.SANPHAM.THUONGHIEU != null
                                ? v.SANPHAM.THUONGHIEU.TenThuongHieu
                                : "-",
                            TenLoai = v.SANPHAM != null && v.SANPHAM.LOAISANPHAM != null
                                ? v.SANPHAM.LOAISANPHAM.TenLoai
                                : "-",
                            CauHinh = FormatVariantSpecs(v),
                            SoDonCoSanPham = lines.Select(x => x.Order.MaDonHang).Distinct().Count(),
                            SoLuongBanRa = qtySold,
                            SoLuongDaGiao = qtyDelivered,
                            DoanhThuBanRa = rev,
                            GiaVonUocTinh = cogs,
                            LoiNhuanUocTinh = rev - cogs,
                            SoLuongTonHienTai = v.TONKHO != null ? v.TONKHO.SoLuongTon : 0,
                            TyTrongDoanhThu = totalLineRevenue > 0
                                ? Math.Round(rev * 100m / totalLineRevenue, 1)
                                : 0m
                        };
                    })
                    .OrderByDescending(x => x.SoLuongBanRa)
                    .ThenByDescending(x => x.DoanhThuBanRa)
                    .ThenBy(x => x.TenSanPham)
                    .ToList();

                // 6. Thống kê theo thương hiệu
                var brandSales = productSales
                    .GroupBy(x => x.TenThuongHieu)
                    .Select(g =>
                    {
                        var rev = g.Sum(i => i.DoanhThuBanRa);
                        return new BrandSalesStatItemViewModel
                        {
                            TenThuongHieu = g.Key,
                            SoLuongBanRa = g.Sum(i => i.SoLuongBanRa),
                            DoanhThu = rev,
                            TyTrongDoanhThu = totalLineRevenue > 0
                                ? Math.Round(rev * 100m / totalLineRevenue, 1)
                                : 0m
                        };
                    })
                    .OrderByDescending(x => x.DoanhThu)
                    .ThenByDescending(x => x.SoLuongBanRa)
                    .ToList();

                // 7. Thống kê theo loại sản phẩm
                var categorySales = productSales
                    .GroupBy(x => x.TenLoai)
                    .Select(g =>
                    {
                        var rev = g.Sum(i => i.DoanhThuBanRa);
                        return new CategorySalesStatItemViewModel
                        {
                            TenLoai = g.Key,
                            SoLuongBanRa = g.Sum(i => i.SoLuongBanRa),
                            DoanhThu = rev,
                            TyTrongDoanhThu = totalLineRevenue > 0
                                ? Math.Round(rev * 100m / totalLineRevenue, 1)
                                : 0m
                        };
                    })
                    .OrderByDescending(x => x.DoanhThu)
                    .ThenByDescending(x => x.SoLuongBanRa)
                    .ToList();

                string periodDesc;
                if (fromDate.HasValue && toDate.HasValue)
                {
                    periodDesc = string.Format("Từ ngày {0:dd/MM/yyyy} đến ngày {1:dd/MM/yyyy}", fromDate.Value, toDate.Value);
                }
                else if (fromDate.HasValue)
                {
                    periodDesc = string.Format("Từ ngày {0:dd/MM/yyyy} đến nay", fromDate.Value);
                }
                else if (toDate.HasValue)
                {
                    periodDesc = string.Format("Đến ngày {0:dd/MM/yyyy}", toDate.Value);
                }
                else
                {
                    periodDesc = "Toàn thời gian hệ thống";
                }

                return new ReportDashboardViewModel
                {
                    Period = normalizedPeriod,
                    FromDate = fromDate,
                    ToDate = toDate,
                    PeriodDescription = periodDesc,
                    TotalValidRevenue = totalValidRevenue,
                    DeliveredRevenue = deliveredRevenue,
                    ProcessingRevenue = processingRevenue,
                    CanceledRevenue = canceledRevenue,
                    EstimatedCostOfGoodsSold = totalCogs,
                    EstimatedGrossProfit = totalValidRevenue - totalCogs,
                    TotalImportCost = totalImportCost,
                    AverageOrderValue = validOrders.Count > 0 ? Math.Round(totalValidRevenue / validOrders.Count, 0) : 0m,
                    TotalOrders = orders.Count,
                    ValidOrdersCount = validOrders.Count,
                    DeliveredOrdersCount = deliveredOrders.Count,
                    CanceledOrdersCount = canceledOrders.Count,
                    TotalUnitsSold = totalUnitsSold,
                    TotalUnitsImported = totalUnitsImported,
                    RevenueByDates = revenueByDates,
                    RevenueByMonths = revenueByMonths,
                    StatusBreakdowns = statusBreakdowns,
                    PaymentBreakdowns = paymentBreakdowns,
                    ProductSales = productSales,
                    BrandSales = brandSales,
                    CategorySales = categorySales
                };
            }
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
                specs.Add(variant.RAMGB.Value + "GB");
            }
            if (variant.BoNhoGB.HasValue)
            {
                specs.Add(variant.BoNhoGB.Value + "GB");
            }
            return specs.Count > 0 ? string.Join(" / ", specs) : variant.SKU;
        }
    }
}

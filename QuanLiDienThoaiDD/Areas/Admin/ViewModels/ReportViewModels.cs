using System;
using System.Collections.Generic;

namespace QuanLiDienThoaiDD.Areas.Admin.ViewModels
{
    public class ReportDashboardViewModel
    {
        public string Period { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string PeriodDescription { get; set; }

        // 1. Thống kê doanh thu tổng quan
        public decimal TotalValidRevenue { get; set; }
        public decimal DeliveredRevenue { get; set; }
        public decimal ProcessingRevenue { get; set; }
        public decimal CanceledRevenue { get; set; }
        public decimal EstimatedCostOfGoodsSold { get; set; }
        public decimal EstimatedGrossProfit { get; set; }
        public decimal TotalImportCost { get; set; }
        public decimal AverageOrderValue { get; set; }

        // 2. Thống kê đơn hàng & số lượng bán
        public int TotalOrders { get; set; }
        public int ValidOrdersCount { get; set; }
        public int DeliveredOrdersCount { get; set; }
        public int CanceledOrdersCount { get; set; }
        public int TotalUnitsSold { get; set; }
        public int TotalUnitsImported { get; set; }

        // 3. Dữ liệu chi tiết
        public IList<RevenueByDateItemViewModel> RevenueByDates { get; set; }
        public IList<RevenueByMonthItemViewModel> RevenueByMonths { get; set; }
        public IList<OrderStatusBreakdownViewModel> StatusBreakdowns { get; set; }
        public IList<PaymentMethodBreakdownViewModel> PaymentBreakdowns { get; set; }
        public IList<ProductSalesStatItemViewModel> ProductSales { get; set; }
        public IList<BrandSalesStatItemViewModel> BrandSales { get; set; }
        public IList<CategorySalesStatItemViewModel> CategorySales { get; set; }
    }

    public class RevenueByDateItemViewModel
    {
        public DateTime Ngay { get; set; }
        public int SoDonHang { get; set; }
        public int SoDonDaGiao { get; set; }
        public int SoDonHuy { get; set; }
        public int SoSanPhamBan { get; set; }
        public decimal TamTinh { get; set; }
        public decimal GiamGia { get; set; }
        public decimal PhiVanChuyen { get; set; }
        public decimal DoanhThuHopLe { get; set; }
        public decimal DoanhThuDaGiao { get; set; }
        public decimal GiaVonUocTinh { get; set; }
        public decimal LoiNhuanUocTinh { get; set; }
    }

    public class RevenueByMonthItemViewModel
    {
        public int Nam { get; set; }
        public int Thang { get; set; }
        public string NhanThang => $"{Thang:00}/{Nam}";
        public int SoDonHang { get; set; }
        public int SoSanPhamBan { get; set; }
        public decimal DoanhThuHopLe { get; set; }
        public decimal DoanhThuDaGiao { get; set; }
        public decimal ChiPhiNhapKho { get; set; }
        public decimal LoiNhuanGop { get; set; }
    }

    public class OrderStatusBreakdownViewModel
    {
        public string TrangThai { get; set; }
        public string TenTrangThai { get; set; }
        public int SoDon { get; set; }
        public decimal TongGiaTri { get; set; }
        public decimal TyLePhanTram { get; set; }
    }

    public class PaymentMethodBreakdownViewModel
    {
        public string PhuongThuc { get; set; }
        public string TenPhuongThuc { get; set; }
        public int SoDon { get; set; }
        public decimal DoanhThu { get; set; }
        public decimal TyLePhanTram { get; set; }
    }

    public class ProductSalesStatItemViewModel
    {
        public string MaBienSanPham { get; set; }
        public string SKU { get; set; }
        public string MaSanPham { get; set; }
        public string TenSanPham { get; set; }
        public string TenThuongHieu { get; set; }
        public string TenLoai { get; set; }
        public string CauHinh { get; set; }
        public int SoDonCoSanPham { get; set; }
        public int SoLuongBanRa { get; set; }
        public int SoLuongDaGiao { get; set; }
        public decimal DoanhThuBanRa { get; set; }
        public decimal GiaVonUocTinh { get; set; }
        public decimal LoiNhuanUocTinh { get; set; }
        public int SoLuongTonHienTai { get; set; }
        public decimal TyTrongDoanhThu { get; set; }
    }

    public class BrandSalesStatItemViewModel
    {
        public string MaThuongHieu { get; set; }
        public string TenThuongHieu { get; set; }
        public int SoLuongBanRa { get; set; }
        public decimal DoanhThu { get; set; }
        public decimal TyTrongDoanhThu { get; set; }
    }

    public class CategorySalesStatItemViewModel
    {
        public string MaLoai { get; set; }
        public string TenLoai { get; set; }
        public int SoLuongBanRa { get; set; }
        public decimal DoanhThu { get; set; }
        public decimal TyTrongDoanhThu { get; set; }
    }
}

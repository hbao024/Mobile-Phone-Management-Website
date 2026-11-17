using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLiDienThoaiDD.Areas.Admin.ViewModels
{
    public class OrderIndexViewModel
    {
        public string Search { get; set; }
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public int TotalOrders { get; set; }
        public int PendingCount { get; set; }
        public int ConfirmedCount { get; set; }
        public int ShippingCount { get; set; }
        public int DeliveredCount { get; set; }
        public int CanceledCount { get; set; }
        public decimal TotalValidRevenue { get; set; }

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public IList<OrderListItemViewModel> Orders { get; set; }
    }

    public class OrderListItemViewModel
    {
        public string MaDonHang { get; set; }
        public string MaKH { get; set; }
        public string TenKhachHang { get; set; }
        public string TenNguoiNhan { get; set; }
        public string SoDienThoaiNhan { get; set; }
        public string DiaChiGiaoHang { get; set; }
        public DateTime NgayDat { get; set; }
        public DateTime NgayCapNhat { get; set; }
        public string TrangThaiDonHang { get; set; }
        public string PhuongThucThanhToan { get; set; }
        public string TrangThaiThanhToan { get; set; }
        public int TongSoSanPham { get; set; }
        public decimal TongTien { get; set; }
        public string TenNVXuLy { get; set; }
    }

    public class OrderDetailViewModel
    {
        public string MaDonHang { get; set; }
        public string MaKH { get; set; }
        public string TenKhachHang { get; set; }
        public string EmailKhachHang { get; set; }
        public string MaNVXuLy { get; set; }
        public string TenNVXuLy { get; set; }
        public DateTime NgayDat { get; set; }
        public DateTime NgayCapNhat { get; set; }
        public string TrangThaiDonHang { get; set; }
        public string PhuongThucThanhToan { get; set; }
        public string TrangThaiThanhToan { get; set; }
        public string TenNguoiNhan { get; set; }
        public string SoDienThoaiNhan { get; set; }
        public string DiaChiGiaoHang { get; set; }
        public decimal TamTinh { get; set; }
        public decimal PhiVanChuyen { get; set; }
        public decimal GiamGia { get; set; }
        public decimal TongTien { get; set; }
        public string GhiChu { get; set; }
        public string LyDoHuy { get; set; }

        public bool CanConfirm { get; set; }
        public bool HasEnoughStockForAllItems { get; set; }
        public bool CanShip { get; set; }
        public bool CanDeliver { get; set; }
        public bool CanCancel { get; set; }

        public IList<OrderDetailItemViewModel> Items { get; set; }
        public IList<OrderStatusHistoryItemViewModel> StatusHistories { get; set; }
        public IList<OrderInventoryLogItemViewModel> InventoryLogs { get; set; }
    }

    public class OrderDetailItemViewModel
    {
        public string MaBienSanPham { get; set; }
        public string SKU { get; set; }
        public string TenSanPham { get; set; }
        public string MauSac { get; set; }
        public short? RAMGB { get; set; }
        public short? BoNhoGB { get; set; }
        public string HinhAnhUrl { get; set; }
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public decimal GiamGia { get; set; }
        public decimal ThanhTien { get; set; }
        public int SoLuongTonHienTai { get; set; }
    }

    public class OrderStatusHistoryItemViewModel
    {
        public long MaLichSu { get; set; }
        public string TrangThaiCu { get; set; }
        public string TrangThaiMoi { get; set; }
        public string NguoiThucHien { get; set; }
        public DateTime ThoiGian { get; set; }
        public string GhiChu { get; set; }
    }

    public class OrderInventoryLogItemViewModel
    {
        public long MaGiaoDich { get; set; }
        public string MaBienSanPham { get; set; }
        public string SKU { get; set; }
        public string LoaiGiaoDich { get; set; }
        public int SoLuong { get; set; }
        public int SoLuongTruoc { get; set; }
        public int SoLuongSau { get; set; }
        public string TenNV { get; set; }
        public DateTime NgayGiaoDich { get; set; }
        public string GhiChu { get; set; }
    }

    public class CreateOrderViewModel
    {
        public string MaKH { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên người nhận.")]
        [StringLength(100)]
        public string TenNguoiNhan { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại nhận hàng.")]
        [StringLength(15)]
        public string SoDienThoaiNhan { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
        [StringLength(300)]
        public string DiaChiGiaoHang { get; set; }

        [Required]
        public string PhuongThucThanhToan { get; set; }

        public decimal PhiVanChuyen { get; set; }
        public decimal GiamGia { get; set; }

        [StringLength(500)]
        public string GhiChu { get; set; }

        public bool XacNhanNgay { get; set; }

        public IList<string> MaBienSanPhams { get; set; }
        public IList<int> SoLuongs { get; set; }

        public IList<CustomerOptionViewModel> AvailableCustomers { get; set; }
        public IList<VariantOptionViewModel> AvailableVariants { get; set; }
    }

    public class CustomerOptionViewModel
    {
        public string MaKH { get; set; }
        public string TenKH { get; set; }
        public string DienThoai { get; set; }
        public string DiaChi { get; set; }
    }

    public class VariantOptionViewModel
    {
        public string MaBienSanPham { get; set; }
        public string SKU { get; set; }
        public string TenHienThi { get; set; }
        public decimal GiaNhap { get; set; }
        public decimal GiaBan { get; set; }
        public int SoLuongTon { get; set; }
    }
}

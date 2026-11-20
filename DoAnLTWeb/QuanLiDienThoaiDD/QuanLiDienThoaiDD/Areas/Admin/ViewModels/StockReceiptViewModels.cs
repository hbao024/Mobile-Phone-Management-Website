using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLiDienThoaiDD.Areas.Admin.ViewModels
{
    public class StockReceiptIndexViewModel
    {
        public string Search { get; set; }
        public string MaNCC { get; set; }
        public string Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public int TotalReceipts { get; set; }
        public int ImportedReceipts { get; set; }
        public int PendingReceipts { get; set; }
        public int CanceledReceipts { get; set; }
        public decimal TotalImportAmount { get; set; }
        public int TotalUnitsInStock { get; set; }

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public IList<SupplierOptionViewModel> Suppliers { get; set; }
        public IList<StockReceiptListItemViewModel> Receipts { get; set; }
        public IList<InventorySnapshotItemViewModel> InventoryItems { get; set; }
        public IList<OrderInventoryLogItemViewModel> RecentLogs { get; set; }
    }

    public class SupplierOptionViewModel
    {
        public string MaNCC { get; set; }
        public string TenNCC { get; set; }
    }

    public class StockReceiptListItemViewModel
    {
        public string MaPhieuNhap { get; set; }
        public string MaNCC { get; set; }
        public string TenNCC { get; set; }
        public string MaNV { get; set; }
        public string TenNV { get; set; }
        public DateTime NgayNhap { get; set; }
        public string TrangThai { get; set; }
        public int SoDongSanPham { get; set; }
        public int TongSoLuongNhap { get; set; }
        public decimal TongTien { get; set; }
        public string GhiChu { get; set; }
    }

    public class InventorySnapshotItemViewModel
    {
        public string MaBienSanPham { get; set; }
        public string SKU { get; set; }
        public string TenSanPham { get; set; }
        public string TenThuongHieu { get; set; }
        public string CauHinh { get; set; }
        public decimal GiaNhap { get; set; }
        public decimal GiaBan { get; set; }
        public int SoLuongTon { get; set; }
        public DateTime NgayCapNhat { get; set; }
    }

    public class StockReceiptDetailViewModel
    {
        public string MaPhieuNhap { get; set; }
        public string MaNCC { get; set; }
        public string TenNCC { get; set; }
        public string DienThoaiNCC { get; set; }
        public string DiaChiNCC { get; set; }
        public string MaNV { get; set; }
        public string TenNV { get; set; }
        public DateTime NgayNhap { get; set; }
        public string TrangThai { get; set; }
        public decimal TongTien { get; set; }
        public string GhiChu { get; set; }

        public bool CanConfirm { get; set; }
        public bool CanCancel { get; set; }

        public IList<StockReceiptDetailItemViewModel> Items { get; set; }
        public IList<OrderInventoryLogItemViewModel> InventoryLogs { get; set; }
    }

    public class StockReceiptDetailItemViewModel
    {
        public string MaBienSanPham { get; set; }
        public string SKU { get; set; }
        public string TenSanPham { get; set; }
        public string CauHinh { get; set; }
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien { get; set; }
        public int SoLuongTonHienTai { get; set; }
    }

    public class CreateStockReceiptViewModel
    {
        [StringLength(20)]
        public string MaPhieuNhap { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nhà cung cấp.")]
        public string MaNCC { get; set; }

        [Required]
        public string TrangThai { get; set; }

        [StringLength(500)]
        public string GhiChu { get; set; }

        public IList<string> MaBienSanPhams { get; set; }
        public IList<int> SoLuongs { get; set; }
        public IList<decimal> DonGias { get; set; }

        public IList<SupplierOptionViewModel> Suppliers { get; set; }
        public IList<VariantOptionViewModel> Variants { get; set; }
    }
}
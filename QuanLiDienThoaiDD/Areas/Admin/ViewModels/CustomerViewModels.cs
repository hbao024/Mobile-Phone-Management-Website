using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLiDienThoaiDD.Areas.Admin.ViewModels
{
    public class CustomerIndexViewModel
    {
        public string Search { get; set; }
        public string StatusFilter { get; set; }
        public string AccountFilter { get; set; }

        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int LockedCustomers { get; set; }
        public int LinkedAccountCustomers { get; set; }
        public decimal TotalCustomerSpending { get; set; }

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public IList<CustomerListItemViewModel> Customers { get; set; }
    }

    public class CustomerListItemViewModel
    {
        public string MaKH { get; set; }
        public string TenKH { get; set; }
        public string Email { get; set; }
        public string DienThoai { get; set; }
        public string DiaChi { get; set; }
        public DateTime NgayThamGia { get; set; }
        public int? MaTaiKhoan { get; set; }
        public string TenDangNhap { get; set; }
        public bool TrangThai { get; set; }
        public int SoDonHang { get; set; }
        public int SoDonThanhCong { get; set; }
        public decimal TongChiTieu { get; set; }
    }

    public class CustomerDetailViewModel
    {
        public string MaKH { get; set; }
        public string TenKH { get; set; }
        public string Email { get; set; }
        public string DienThoai { get; set; }
        public string DiaChi { get; set; }
        public DateTime NgayThamGia { get; set; }
        public int? MaTaiKhoan { get; set; }
        public string TenDangNhap { get; set; }
        public bool? TrangThaiTaiKhoan { get; set; }
        public bool TrangThai { get; set; }

        public int TongSoDonHang { get; set; }
        public int SoDonHoanThanh { get; set; }
        public int SoDonDangXuLy { get; set; }
        public int SoDonDaHuy { get; set; }
        public decimal TongChiTieuHopLe { get; set; }

        public IList<OrderListItemViewModel> Orders { get; set; }
        public IList<CustomerReviewItemViewModel> Reviews { get; set; }
    }

    public class CustomerReviewItemViewModel
    {
        public long MaDanhGia { get; set; }
        public string MaDonHang { get; set; }
        public string TenSanPham { get; set; }
        public string SKU { get; set; }
        public byte SoSao { get; set; }
        public string NoiDung { get; set; }
        public DateTime NgayDanhGia { get; set; }
        public bool TrangThai { get; set; }
    }

    public class CustomerFormViewModel
    {
        [StringLength(10)]
        public string MaKH { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên khách hàng.")]
        [StringLength(100, ErrorMessage = "Tên khách hàng tối đa 100 ký tự.")]
        public string TenKH { get; set; }

        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        [StringLength(255)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [StringLength(15, ErrorMessage = "Số điện thoại tối đa 15 ký tự.")]
        public string DienThoai { get; set; }

        [StringLength(300, ErrorMessage = "Địa chỉ tối đa 300 ký tự.")]
        public string DiaChi { get; set; }

        public bool TrangThai { get; set; } = true;
    }
}

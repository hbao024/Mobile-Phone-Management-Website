using System.ComponentModel.DataAnnotations;

namespace QuanLiDienThoaiDD.Areas.Admin.ViewModels
{
    public class SupplierEditViewModel
    {
        [Display(Name = "Tên nhà cung cấp")]
        [Required(ErrorMessage = "Vui lòng nhập tên nhà cung cấp.")]
        [StringLength(150, ErrorMessage = "Tên nhà cung cấp không được vượt quá 150 ký tự.")]
        public string TenNCC { get; set; }

        [Display(Name = "Địa chỉ")]
        [StringLength(300, ErrorMessage = "Địa chỉ không được vượt quá 300 ký tự.")]
        public string DiaChi { get; set; }

        [Display(Name = "Điện thoại")]
        [StringLength(15, ErrorMessage = "Điện thoại không được vượt quá 15 ký tự.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [RegularExpression(@"[0-9+(). -]+", ErrorMessage = "Điện thoại chỉ gồm số 0-9 và các dấu + ( ) . - hoặc khoảng trắng.")]
        public string DienThoai { get; set; }

        [Display(Name = "Email")]
        [StringLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự.")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        [RegularExpression(@"[\x21-\x7E]+", ErrorMessage = "Email dùng ký tự ASCII không có khoảng trắng; tên miền quốc tế cần dùng dạng Punycode.")]
        public string Email { get; set; }

        [Display(Name = "Đang sử dụng")]
        public bool TrangThai { get; set; }
    }

    public class SupplierViewModel : SupplierEditViewModel
    {
        [Display(Name = "Mã nhà cung cấp")]
        [Required(ErrorMessage = "Vui lòng nhập mã nhà cung cấp.")]
        [StringLength(10, ErrorMessage = "Mã nhà cung cấp không được vượt quá 10 ký tự.")]
        [RegularExpression(@"[A-Za-z0-9_-]+", ErrorMessage = "Mã nhà cung cấp chỉ gồm chữ không dấu, số, dấu gạch dưới hoặc gạch ngang.")]
        public string MaNCC { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace QuanLiDienThoaiDD.Areas.Admin.ViewModels
{
    public class CategoryEditViewModel
    {
        [Display(Name = "Tên loại")]
        [Required(ErrorMessage = "Vui lòng nhập tên loại.")]
        [StringLength(100, ErrorMessage = "Tên loại không được vượt quá 100 ký tự.")]
        public string TenLoai { get; set; }

        [Display(Name = "Mô tả")]
        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
        public string MoTa { get; set; }

        [Display(Name = "Đang sử dụng")]
        public bool TrangThai { get; set; }
    }

    public class CategoryViewModel : CategoryEditViewModel
    {
        [Display(Name = "Mã loại")]
        [Required(ErrorMessage = "Vui lòng nhập mã loại.")]
        [StringLength(10, ErrorMessage = "Mã loại không được vượt quá 10 ký tự.")]
        [RegularExpression(@"[A-Za-z0-9_-]+", ErrorMessage = "Mã loại chỉ gồm chữ không dấu, số, dấu gạch dưới hoặc gạch ngang.")]
        public string MaLoai { get; set; }
    }
}

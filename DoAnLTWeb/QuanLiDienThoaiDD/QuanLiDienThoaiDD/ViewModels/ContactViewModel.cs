using System.ComponentModel.DataAnnotations;

namespace QuanLiDienThoaiDD.ViewModels
{
    public class ContactViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(150)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung cần hỗ trợ.")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Nội dung cần từ 10 đến 1000 ký tự.")]
        [Display(Name = "Nội dung")]
        public string NoiDung { get; set; }
    }
}

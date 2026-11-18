using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace QuanLiDienThoaiDD.Areas.Admin.ViewModels
{
    public class BrandEditViewModel
    {
        [Display(Name = "Tên thương hiệu")]
        [Required(ErrorMessage = "Vui lòng nhập tên thương hiệu.")]
        [StringLength(100, ErrorMessage = "Tên thương hiệu không được vượt quá 100 ký tự.")]
        public string TenThuongHieu { get; set; }

        [Display(Name = "Mô tả")]
        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
        public string MoTa { get; set; }

        [Display(Name = "Đường dẫn logo")]
        [StringLength(500, ErrorMessage = "Đường dẫn logo không được vượt quá 500 ký tự.")]
        [LogoPath]
        public string LogoUrl { get; set; }

        [Display(Name = "Đang sử dụng")]
        public bool TrangThai { get; set; }
    }

    public class BrandViewModel : BrandEditViewModel
    {
        [Display(Name = "Mã thương hiệu")]
        [Required(ErrorMessage = "Vui lòng nhập mã thương hiệu.")]
        [StringLength(10, ErrorMessage = "Mã thương hiệu không được vượt quá 10 ký tự.")]
        [RegularExpression(@"[A-Za-z0-9_-]+", ErrorMessage = "Mã thương hiệu chỉ gồm chữ không dấu, số, dấu gạch dưới hoặc gạch ngang.")]
        public string MaThuongHieu { get; set; }
    }

    public sealed class LogoPathAttribute : ValidationAttribute
    {
        public LogoPathAttribute()
            : base("Nhập URL HTTP/HTTPS không có thông tin đăng nhập hoặc đường dẫn /images/... hay ~/images/...; không dùng đường dẫn lùi, dấu gạch chéo ngược hoặc khoảng trắng.") { }

        public override bool IsValid(object value)
        {
            var path = value as string;
            if (string.IsNullOrWhiteSpace(path)) return true;
            path = path.Trim();
            if (path.Contains("\\") || path.Any(char.IsWhiteSpace) || path.Any(char.IsControl)) return false;

            Uri uri;
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return Uri.IsWellFormedUriString(path, UriKind.Absolute)
                    && Uri.TryCreate(path, UriKind.Absolute, out uri)
                    && !string.IsNullOrEmpty(uri.Host) && string.IsNullOrEmpty(uri.UserInfo);

            var local = path.StartsWith("~/", StringComparison.Ordinal) ? path.Substring(1) : path;
            if (!local.StartsWith("/images/", StringComparison.Ordinal)
                || !Uri.IsWellFormedUriString(local, UriKind.Relative)) return false;
            local = Uri.UnescapeDataString(local);
            return local.Length > "/images/".Length && !local.Contains("\\") && !local.Contains("?") && !local.Contains("#")
                && !local.Any(char.IsControl) && !local.Split('/').Any(x => x == ".." || x == ".");
        }
    }
}

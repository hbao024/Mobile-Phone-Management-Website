using System.ComponentModel.DataAnnotations;
using System.Web;

namespace QuanLiDienThoaiDD.Areas.Admin.ViewModels
{
    public class ProductEditViewModel
    {
        [Display(Name = "Tên sản phẩm")]
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm tối đa 200 ký tự.")]
        public string TenSanPham { get; set; }

        [Display(Name = "Loại sản phẩm")]
        [Required(ErrorMessage = "Vui lòng chọn loại sản phẩm.")]
        [StringLength(10)]
        public string MaLoai { get; set; }

        [Display(Name = "Thương hiệu")]
        [Required(ErrorMessage = "Vui lòng chọn thương hiệu.")]
        [StringLength(10)]
        public string MaThuongHieu { get; set; }

        [Display(Name = "Nhà cung cấp mặc định")]
        [StringLength(10)]
        public string MaNCCMacDinh { get; set; }

        [Display(Name = "Mô tả")]
        public string MoTa { get; set; }

        [Display(Name = "Bảo hành (tháng)")]
        [Required(ErrorMessage = "Vui lòng nhập số tháng bảo hành.")]
        [Range(0, 120, ErrorMessage = "Bảo hành từ 0 đến 120 tháng.")]
        public short? BaoHanhThang { get; set; }

        [Display(Name = "Đang kinh doanh")]
        public bool TrangThai { get; set; }
    }

    public class ProductViewModel : ProductEditViewModel
    {
        [Display(Name = "Mã sản phẩm")]
        [Required(ErrorMessage = "Vui lòng nhập mã sản phẩm.")]
        [StringLength(20, ErrorMessage = "Mã sản phẩm tối đa 20 ký tự.")]
        [RegularExpression(@"[A-Za-z0-9_-]+", ErrorMessage = "Mã chỉ gồm chữ ASCII, số, _ hoặc -.")]
        public string MaSanPham { get; set; }
    }

    public class ProductVariantEditViewModel
    {
        [Display(Name = "SKU")]
        [Required(ErrorMessage = "Vui lòng nhập SKU.")]
        [StringLength(50, ErrorMessage = "SKU tối đa 50 ký tự.")]
        [RegularExpression(@"[\x21-\x7E]+", ErrorMessage = "SKU dùng ký tự ASCII không có khoảng trắng.")]
        public string SKU { get; set; }

        [Display(Name = "Màu sắc")]
        [StringLength(50, ErrorMessage = "Màu sắc tối đa 50 ký tự.")]
        public string MauSac { get; set; }

        [Display(Name = "RAM (GB)")]
        [Range(1, short.MaxValue, ErrorMessage = "RAM từ 1 đến 32767 GB hoặc để trống.")]
        public short? RAMGB { get; set; }

        [Display(Name = "Bộ nhớ (GB)")]
        [Range(1, short.MaxValue, ErrorMessage = "Bộ nhớ từ 1 đến 32767 GB hoặc để trống.")]
        public short? BoNhoGB { get; set; }

        [Display(Name = "Giá nhập")]
        [Required(ErrorMessage = "Vui lòng nhập giá nhập.")]
        [SqlCatalogPrice]
        public decimal? GiaNhap { get; set; }

        [Display(Name = "Giá bán")]
        [Required(ErrorMessage = "Vui lòng nhập giá bán.")]
        [SqlCatalogPrice]
        public decimal? GiaBan { get; set; }

        [Display(Name = "Đường dẫn ảnh")]
        [StringLength(500, ErrorMessage = "Đường dẫn ảnh tối đa 500 ký tự.")]
        [RegularExpression(@"/images/(?:[A-Za-z0-9_-]+/)*[A-Za-z0-9_-][A-Za-z0-9_.-]*", ErrorMessage = "Dùng /images/ten-anh.jpg; thư mục/tên ảnh chỉ gồm chữ ASCII, số, _ hoặc -, tên ảnh có thể chứa dấu chấm. Không nhập URL ngoài hay đường dẫn lùi.")]
        public string HinhAnhUrl { get; set; }

        [Display(Name = "Tải ảnh mới")]
        public HttpPostedFileBase ImageUpload { get; set; }

        [Display(Name = "Bỏ ảnh của biến thể")]
        public bool RemoveImage { get; set; }

        [Display(Name = "Đang kinh doanh")]
        public bool TrangThai { get; set; }
    }

    public class ProductVariantViewModel : ProductVariantEditViewModel
    {
        [Display(Name = "Mã biến thể")]
        [Required(ErrorMessage = "Vui lòng nhập mã biến thể.")]
        [StringLength(30, ErrorMessage = "Mã biến thể tối đa 30 ký tự.")]
        [RegularExpression(@"[A-Za-z0-9_-]+", ErrorMessage = "Mã chỉ gồm chữ ASCII, số, _ hoặc -.")]
        public string MaBienSanPham { get; set; }
    }

    public sealed class SqlCatalogPriceAttribute : ValidationAttribute
    {
        public SqlCatalogPriceAttribute()
            : base("Giá từ 0 đến 9999999999999999.99, tối đa 2 chữ số thập phân.") { }

        public override bool IsValid(object value)
        {
            if (value == null) return true;
            if (!(value is decimal)) return false;
            var amount = (decimal)value;
            return amount >= 0 && amount <= 9999999999999999.99m && decimal.Round(amount, 2) == amount;
        }
    }
}

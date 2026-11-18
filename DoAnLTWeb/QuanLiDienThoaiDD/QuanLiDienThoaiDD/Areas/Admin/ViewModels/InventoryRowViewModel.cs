using System;

namespace QuanLiDienThoaiDD.Areas.Admin.ViewModels
{
    public class InventoryRowViewModel
    {
        public string MaBienSanPham { get; set; }
        public string SKU { get; set; }
        public string MaSanPham { get; set; }
        public string TenSanPham { get; set; }
        public bool VariantActive { get; set; }
        public bool ProductActive { get; set; }
        public int? SoLuongTon { get; set; }
        public int? SoLuongDangDat { get; set; }
        public DateTime? NgayCapNhat { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanLiDienThoaiDD.Models
{
    public class CartItem
    {
        public string MaBienSanPham { get; set; }
        public string TenSanPham { get; set; }
        public string MauSac { get; set; }
        public string BoNho { get; set; }
        public string HinhAnhUrl { get; set; }
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }

        public decimal ThanhTien
        {
            get { return SoLuong * DonGia; }
        }

        public CartItem(string maBienSanPham)
        {
            QLHANGHOA_PHUKIEN_WEBEntities data = new QLHANGHOA_PHUKIEN_WEBEntities();

            BIENSANPHAM bsp = data.BIENSANPHAMs.FirstOrDefault(b => b.MaBienSanPham == maBienSanPham);

            if (bsp != null)
            {
                MaBienSanPham = bsp.MaBienSanPham;
                TenSanPham = bsp.SANPHAM.TenSanPham;
                MauSac = bsp.MauSac;
                BoNho = bsp.BoNhoGB.HasValue ? bsp.BoNhoGB + " GB" : "";
                HinhAnhUrl = bsp.HinhAnhUrl;
                DonGia = bsp.GiaBan;
                SoLuong = 1;
            }
        }
    }
}
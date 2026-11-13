using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanLiDienThoaiDD.Models
{
    public class GioHangTam
    {
        public List<CartItem> lst;
        public GioHangTam()
        {
            lst = new List<CartItem>();
        }
        public int SoMatHang()
        {
            return lst.Count;
        }
        public int TongSLHang()
        {
            return lst.Sum(n => n.SoLuong);
        }
        public decimal TongThanhTien()
        {
            return lst.Sum(n => n.ThanhTien);
        }
        public int Them(string maBienSanPham)
        {
            CartItem sanpham = lst.Find(n => n.MaBienSanPham == maBienSanPham);

            if (sanpham == null) 
            {
                CartItem moi = new CartItem(maBienSanPham);
                if (moi.TenSanPham == null) 
                {
                    return -1;
                }
                lst.Add(moi);
            }
            else 
            {
                sanpham.SoLuong++;
            }

            return 1;
        }

        public void CapNhat(string maBienSanPham, int soLuong)
        {
            CartItem sanpham = lst.Find(n => n.MaBienSanPham == maBienSanPham);

            if (sanpham != null)
            {
                if (soLuong <= 0)
                {
                    lst.Remove(sanpham); 
                }
                else
                {
                    sanpham.SoLuong = soLuong;
                }
            }
        }

        public void Xoa(string maBienSanPham)
        {
            CartItem sanpham = lst.Find(n => n.MaBienSanPham == maBienSanPham);

            if (sanpham != null)
            {
                lst.Remove(sanpham);
            }
        }

        public void XoaTatCa()
        {
            lst.Clear();
        }
    }
}
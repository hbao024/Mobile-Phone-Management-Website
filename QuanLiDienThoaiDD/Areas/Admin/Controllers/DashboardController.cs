using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using QuanLiDienThoaiDD.Areas.Admin.ViewModels;
using QuanLiDienThoaiDD.Models;

namespace QuanLiDienThoaiDD.Areas.Admin.Controllers
{
    [Authorize(Roles = "ADMIN,NHANVIEN")]
    public class DashboardController : Controller
    {
        public ActionResult Index()
        {
            using (var db = new QLHANGHOA_PHUKIEN_WEBEntities())
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                var model = new DashboardViewModel
                {
                    TotalProducts = db.SANPHAMs.Count(x => x.TrangThai),
                    TotalCustomers = db.KHACHHANGs.Count(x => x.TrangThai),
                    PendingOrders = db.DONHANGs.Count(x => x.TrangThaiDonHang == "CHO_XAC_NHAN"),
                    TodayOrders = db.DONHANGs.Count(x => x.NgayDat >= today && x.NgayDat < tomorrow),
                    TodayRevenue = db.DONHANGs
                        .Where(x => x.NgayDat >= today && x.NgayDat < tomorrow
                            && x.TrangThaiDonHang != "DA_HUY")
                        .Select(x => (decimal?)x.TongTien)
                        .Sum() ?? 0m,
                    LowStockItems = db.TONKHOes
                        .Where(x => x.SoLuongTon <= 5 && x.BIENSANPHAM.TrangThai && x.BIENSANPHAM.SANPHAM.TrangThai)
                        .OrderBy(x => x.SoLuongTon)
                        .ThenBy(x => x.BIENSANPHAM.SANPHAM.TenSanPham)
                        .Take(8)
                        .Select(x => new LowStockItemViewModel
                        {
                            VariantId = x.MaBienSanPham,
                            ProductName = x.BIENSANPHAM.SANPHAM.TenSanPham,
                            Sku = x.BIENSANPHAM.SKU,
                            Stock = x.SoLuongTon,
                            ImageUrl = x.BIENSANPHAM.HinhAnhUrl
                        })
                        .ToList(),
                    RecentOrders = db.DONHANGs
                        .Include(x => x.KHACHHANG)
                        .OrderByDescending(x => x.NgayDat)
                        .Take(8)
                        .Select(x => new RecentOrderViewModel
                        {
                            OrderId = x.MaDonHang,
                            CustomerName = x.KHACHHANG == null ? x.TenNguoiNhan : x.KHACHHANG.TenKH,
                            Status = x.TrangThaiDonHang,
                            Total = x.TongTien,
                            CreatedAt = x.NgayDat
                        })
                        .ToList()
                };

                return View(model);
            }
        }
    }
}

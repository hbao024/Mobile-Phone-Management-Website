using System;
using System.Collections.Generic;

namespace QuanLiDienThoaiDD.Areas.Admin.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
        public int PendingOrders { get; set; }
        public int TodayOrders { get; set; }
        public decimal TodayRevenue { get; set; }
        public IList<LowStockItemViewModel> LowStockItems { get; set; }
        public IList<RecentOrderViewModel> RecentOrders { get; set; }
    }

    public class LowStockItemViewModel
    {
        public string VariantId { get; set; }
        public string ProductName { get; set; }
        public string Sku { get; set; }
        public int Stock { get; set; }
        public string ImageUrl { get; set; }
    }

    public class RecentOrderViewModel
    {
        public string OrderId { get; set; }
        public string CustomerName { get; set; }
        public string Status { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

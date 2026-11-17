using System;
using System.Collections.Generic;
using System.Linq;
using QuanLiDienThoaiDD.Models;

namespace QuanLiDienThoaiDD.Areas.Admin.ViewModels
{
    public static class AdminStatusHelper
    {
        public static readonly string[] OrderStatuses =
        {
            "CHO_XAC_NHAN",
            "DA_XAC_NHAN",
            "DANG_GIAO",
            "DA_GIAO",
            "DA_HUY"
        };

        public static readonly string[] PaymentStatuses =
        {
            "CHUA_THANH_TOAN",
            "DA_THANH_TOAN",
            "HOAN_TIEN"
        };

        public static readonly string[] PaymentMethods =
        {
            "COD",
            "CHUYEN_KHOAN"
        };

        public static readonly string[] ReceiptStatuses =
        {
            "CHO_XU_LY",
            "DA_NHAP",
            "DA_HUY"
        };

        public static string GetOrderStatusLabel(string status)
        {
            switch ((status ?? string.Empty).ToUpperInvariant())
            {
                case "CHO_XAC_NHAN":
                    return "Chờ xác nhận";
                case "DA_XAC_NHAN":
                    return "Đã xác nhận";
                case "DANG_GIAO":
                    return "Đang giao hàng";
                case "DA_GIAO":
                    return "Đã giao thành công";
                case "DA_HUY":
                    return "Đã hủy";
                default:
                    return status ?? "Không xác định";
            }
        }

        public static string GetOrderStatusBadgeClass(string status)
        {
            switch ((status ?? string.Empty).ToUpperInvariant())
            {
                case "CHO_XAC_NHAN":
                    return "status-cho_xac_nhan";
                case "DA_XAC_NHAN":
                    return "status-da_xac_nhan";
                case "DANG_GIAO":
                    return "status-dang_giao";
                case "DA_GIAO":
                    return "status-da_giao";
                case "DA_HUY":
                    return "status-da_huy";
                default:
                    return string.Empty;
            }
        }

        public static string GetPaymentStatusLabel(string status)
        {
            switch ((status ?? string.Empty).ToUpperInvariant())
            {
                case "CHUA_THANH_TOAN":
                    return "Chưa thanh toán";
                case "DA_THANH_TOAN":
                    return "Đã thanh toán";
                case "HOAN_TIEN":
                    return "Đã hoàn tiền";
                default:
                    return status ?? "Không xác định";
            }
        }

        public static string GetPaymentStatusBadgeClass(string status)
        {
            switch ((status ?? string.Empty).ToUpperInvariant())
            {
                case "CHUA_THANH_TOAN":
                    return "status-cho_xac_nhan";
                case "DA_THANH_TOAN":
                    return "status-da_giao";
                case "HOAN_TIEN":
                    return "status-da_huy";
                default:
                    return string.Empty;
            }
        }

        public static string GetPaymentMethodLabel(string method)
        {
            switch ((method ?? string.Empty).ToUpperInvariant())
            {
                case "COD":
                    return "Tiền mặt (COD)";
                case "CHUYEN_KHOAN":
                    return "Chuyển khoản";
                default:
                    return method ?? "Không xác định";
            }
        }

        public static string GetReceiptStatusLabel(string status)
        {
            switch ((status ?? string.Empty).ToUpperInvariant())
            {
                case "CHO_XU_LY":
                    return "Chờ xử lý";
                case "DA_NHAP":
                    return "Đã nhập kho";
                case "DA_HUY":
                    return "Đã hủy";
                default:
                    return status ?? "Không xác định";
            }
        }

        public static string GetReceiptStatusBadgeClass(string status)
        {
            switch ((status ?? string.Empty).ToUpperInvariant())
            {
                case "CHO_XU_LY":
                    return "status-cho_xac_nhan";
                case "DA_NHAP":
                    return "status-da_giao";
                case "DA_HUY":
                    return "status-da_huy";
                default:
                    return string.Empty;
            }
        }

        public static string GetInventoryTxTypeLabel(string type)
        {
            switch ((type ?? string.Empty).ToUpperInvariant())
            {
                case "NHAP":
                    return "Nhập kho";
                case "BAN":
                    return "Xuất bán";
                case "TRA_HANG":
                    return "Hoàn trả kho";
                case "DIEU_CHINH_TANG":
                    return "Điều chỉnh tăng";
                case "DIEU_CHINH_GIAM":
                    return "Điều chỉnh giảm";
                default:
                    return type ?? "Khác";
            }
        }

        public static IList<string> GetAllowedNextOrderStatuses(string currentStatus)
        {
            switch ((currentStatus ?? string.Empty).ToUpperInvariant())
            {
                case "CHO_XAC_NHAN":
                    return new[] { "CHO_XAC_NHAN", "DA_XAC_NHAN", "DA_HUY" };
                case "DA_XAC_NHAN":
                    return new[] { "DA_XAC_NHAN", "DANG_GIAO", "DA_HUY" };
                case "DANG_GIAO":
                    return new[] { "DANG_GIAO", "DA_GIAO", "DA_HUY" };
                case "DA_GIAO":
                    return new[] { "DA_GIAO" };
                case "DA_HUY":
                    return new[] { "DA_HUY" };
                default:
                    return new[] { currentStatus };
            }
        }

        public static void ResolveCurrentActor(
            QLHANGHOA_PHUKIEN_WEBEntities db,
            string identityName,
            out int? maTaiKhoan,
            out string maNV)
        {
            maTaiKhoan = null;
            maNV = null;

            if (!string.IsNullOrWhiteSpace(identityName))
            {
                var account = db.TAIKHOANs.FirstOrDefault(x => x.TenDangNhap == identityName);
                if (account != null)
                {
                    maTaiKhoan = account.MaTaiKhoan;
                    var staff = db.NHANVIENs.FirstOrDefault(x => x.MaTaiKhoan == account.MaTaiKhoan && x.TrangThai);
                    if (staff != null)
                    {
                        maNV = staff.MaNV;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(maNV))
            {
                var fallbackStaff = db.NHANVIENs.Where(x => x.TrangThai).OrderBy(x => x.MaNV).FirstOrDefault();
                if (fallbackStaff != null)
                {
                    maNV = fallbackStaff.MaNV;
                }
            }
        }
    }
}

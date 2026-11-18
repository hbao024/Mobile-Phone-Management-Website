using System.ComponentModel.DataAnnotations;

namespace QuanLiDienThoaiDD.Areas.Admin.ViewModels
{
    public class InventoryAdjustmentViewModel
    {
        [Required, RegularExpression("DIEU_CHINH_TANG|DIEU_CHINH_GIAM")]
        public string Type { get; set; }
        [Required, Range(1, int.MaxValue)]
        public int? Quantity { get; set; }
        [Required, StringLength(450)]
        public string Reason { get; set; }
        [Required, StringLength(2048)]
        public string OperationToken { get; set; }
    }
}

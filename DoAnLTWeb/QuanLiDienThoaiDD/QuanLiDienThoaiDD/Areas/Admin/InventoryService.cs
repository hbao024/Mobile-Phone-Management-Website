using System;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace QuanLiDienThoaiDD.Areas.Admin
{
    public sealed class InventoryChange
    {
        public Guid OperationId { get; set; }
        public string VariantId { get; set; }
        public string Type { get; set; }
        public int Quantity { get; set; }
        public string StaffId { get; set; }
        public string Reason { get; set; }
        public string ReceiptId { get; set; }
        public string OrderId { get; set; }

        public void Validate()
        {
            if (OperationId == Guid.Empty || string.IsNullOrWhiteSpace(VariantId) || VariantId.Length > 30
                || string.IsNullOrWhiteSpace(StaffId) || StaffId.Length > 10 || Quantity <= 0
                || string.IsNullOrWhiteSpace(Reason) || Reason.Length > 450)
                throw new ArgumentException("Thông tin giao dịch kho không hợp lệ.");
            if (!new[] { "NHAP", "BAN", "TRA_HANG", "DIEU_CHINH_TANG", "DIEU_CHINH_GIAM" }.Contains(Type))
                throw new ArgumentException("Loại giao dịch kho không hợp lệ.");
            if (Type == "NHAP" ? string.IsNullOrWhiteSpace(ReceiptId) || OrderId != null
                : Type == "BAN" || Type == "TRA_HANG" ? string.IsNullOrWhiteSpace(OrderId) || ReceiptId != null
                : ReceiptId != null || OrderId != null)
                throw new ArgumentException("Liên kết chứng từ không hợp lệ.");
            if ((ReceiptId != null && ReceiptId.Length > 20) || (OrderId != null && OrderId.Length > 20))
                throw new ArgumentException("Mã chứng từ quá dài.");
        }
    }

    public sealed class InventoryResult
    {
        public long LogId { get; set; }
        public int Before { get; set; }
        public int After { get; set; }
        public bool Replayed { get; set; }
    }

    public sealed class InventoryService
    {
        // Caller owns commit/rollback and must not later SaveChanges stale tracked TONKHO entities.
        public InventoryResult Apply(DbTransaction transaction, InventoryChange change)
        {
            if (change == null) throw new ArgumentNullException("change");
            change.Validate();
            if (transaction == null || transaction.Connection == null || transaction.Connection.State != ConnectionState.Open)
                throw new InvalidOperationException("Cần transaction đang hoạt động từ bên gọi.");
            if (transaction.IsolationLevel != IsolationLevel.ReadCommitted && transaction.IsolationLevel != IsolationLevel.Serializable)
                throw new InvalidOperationException("Transaction kho cần ReadCommitted hoặc Serializable.");
            using (var command = transaction.Connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = Batch;
                Add(command, "@variant", DbType.AnsiString, change.VariantId, 30);
                Add(command, "@type", DbType.AnsiString, change.Type, 25);
                Add(command, "@qty", DbType.Int32, change.Quantity);
                Add(command, "@delta", DbType.Int32, change.Type == "BAN" || change.Type == "DIEU_CHINH_GIAM" ? -change.Quantity : change.Quantity);
                Add(command, "@staff", DbType.AnsiString, change.StaffId, 10);
                Add(command, "@receipt", DbType.AnsiString, change.ReceiptId, 20);
                Add(command, "@order", DbType.AnsiString, change.OrderId, 20);
                var marker = "INV1:" + change.OperationId.ToString("N");
                Add(command, "@marker", DbType.String, marker, 37);
                Add(command, "@note", DbType.String, marker + "|" + change.Reason, 500);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) throw new InvalidOperationException("Không nhận được kết quả cập nhật kho.");
                    return new InventoryResult { LogId = reader.GetInt64(0), Before = reader.GetInt32(1), After = reader.GetInt32(2), Replayed = reader.GetBoolean(3) };
                }
            }
        }

        private static void Add(DbCommand command, string name, DbType type, object value, int size = 0)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name; parameter.DbType = type; parameter.Value = value ?? DBNull.Value;
            if (size != 0) parameter.Size = size;
            command.Parameters.Add(parameter);
        }

        internal const string Batch = @"
SET XACT_ABORT ON;
SET NOCOUNT ON;
IF @@TRANCOUNT = 0 THROW 50001, 'Transaction required', 1;
DECLARE @lock int;
EXEC @lock = sys.sp_getapplock @Resource = @marker, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
IF @lock < 0 THROW 50002, 'Request lock unavailable', 1;
IF EXISTS (SELECT 1 FROM dbo.GIAODICHKHO WHERE LEFT(GhiChu, 37) = @marker)
BEGIN
    IF (SELECT COUNT(*) FROM dbo.GIAODICHKHO WHERE LEFT(GhiChu, 37) = @marker) <> 1
       OR NOT EXISTS (SELECT 1 FROM dbo.GIAODICHKHO WHERE LEFT(GhiChu, 37) = @marker
           AND MaBienSanPham = @variant AND LoaiGiaoDich = @type AND SoLuong = @qty AND MaNV = @staff
           AND ISNULL(MaPhieuNhap, '') = ISNULL(@receipt, '') AND ISNULL(MaDonHang, '') = ISNULL(@order, '')
           AND DATALENGTH(GhiChu) = DATALENGTH(@note) AND GhiChu COLLATE Latin1_General_100_BIN2 = @note COLLATE Latin1_General_100_BIN2)
        THROW 50003, 'Request payload conflict', 1;
    SELECT MaGiaoDich, SoLuongTruoc, SoLuongSau, CAST(1 AS bit) FROM dbo.GIAODICHKHO WHERE LEFT(GhiChu, 37) = @marker;
    RETURN;
END;
IF NOT EXISTS (SELECT 1 FROM dbo.BIENSANPHAM WHERE MaBienSanPham = @variant)
    THROW 50004, 'Variant missing', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.NHANVIEN WHERE MaNV = @staff AND TrangThai = 1)
    THROW 50005, 'Staff missing or inactive', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.TONKHO WITH (UPDLOCK, HOLDLOCK) WHERE MaBienSanPham = @variant)
BEGIN
    IF @delta < 0 THROW 50006, 'Stock missing', 1;
    INSERT dbo.TONKHO(MaBienSanPham, SoLuongTon, SoLuongDangDat, NgayCapNhat) VALUES (@variant, 0, 0, SYSDATETIME());
END;
DECLARE @changed TABLE (BeforeQty int, AfterQty int);
UPDATE dbo.TONKHO SET SoLuongTon = SoLuongTon + @delta, NgayCapNhat = SYSDATETIME()
OUTPUT deleted.SoLuongTon, inserted.SoLuongTon INTO @changed
WHERE MaBienSanPham = @variant AND CAST(SoLuongTon AS bigint) + @delta BETWEEN 0 AND 2147483647;
IF NOT EXISTS (SELECT 1 FROM @changed) THROW 50007, 'Insufficient stock or integer overflow', 1;
INSERT dbo.GIAODICHKHO(MaBienSanPham, LoaiGiaoDich, SoLuong, SoLuongTruoc, SoLuongSau, MaPhieuNhap, MaDonHang, MaNV, NgayGiaoDich, GhiChu)
SELECT @variant, @type, @qty, BeforeQty, AfterQty, @receipt, @order, @staff, SYSDATETIME(), @note FROM @changed;
SELECT CAST(SCOPE_IDENTITY() AS bigint), BeforeQty, AfterQty, CAST(0 AS bit) FROM @changed;";
    }
}

// Mock ADO transaction executes an in-memory analogue, NOT SQL Server. No claim of SQL concurrency/rollback acceptance.
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Web.Mvc;
using QuanLiDienThoaiDD.Areas.Admin;
using QuanLiDienThoaiDD.Areas.Admin.Controllers;
using QuanLiDienThoaiDD.Areas.Admin.ViewModels;
using QuanLiDienThoaiDD.Models;

static class AdjustmentChecks
{
    static int count;
    static void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL: " + name); count++; Console.WriteLine("PASS: " + name); }
    static InventoryChange Change(string type = "DIEU_CHINH_TANG", int qty = 3)
    { return new InventoryChange { OperationId = Guid.NewGuid(), VariantId = "V", StaffId = "NV", Type = type, Quantity = qty, Reason = "Count reconciliation" }; }
    static bool Reject(Action action) { try { action(); return false; } catch (ArgumentException) { return true; } catch (InvalidOperationException) { return true; } }
    public static void Run()
    {
        var service = new InventoryService();
        foreach (var qty in new[] { 0, -1 }) Check(Reject(() => Change(qty: qty).Validate()), "reject nonpositive quantity " + qty);
        foreach (var type in new[] { "UPDATE", "", null }) Check(Reject(() => Change(type).Validate()), "reject invalid transaction type");
        var invalid = Change(); invalid.Reason = " "; Check(Reject(invalid.Validate), "reject blank reason");
        invalid = Change(); invalid.Reason = new string('x', 451); Check(Reject(invalid.Validate), "reject long reason");
        invalid = Change(); invalid.OperationId = Guid.Empty; Check(Reject(invalid.Validate), "reject missing operation ID");
        invalid = Change(); invalid.StaffId = ""; Check(Reject(invalid.Validate), "reject missing actor");
        Check(Reject(() => service.Apply(null, Change())), "service requires caller transaction");
        var source = (string)typeof(InventoryService).GetField("Batch", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        Check(source.Contains("UPDLOCK, HOLDLOCK") && source.Contains("SET XACT_ABORT ON"), "SQL source locks missing-key range and aborts runtime errors");
        Check(source.Contains("OUTPUT deleted.SoLuongTon, inserted.SoLuongTon") && source.Contains("CAST(SoLuongTon AS bigint) + @delta BETWEEN 0 AND 2147483647"), "SQL source captures before/after and guards bounds");
        Check(!source.Contains("COMMIT") && !source.Contains("BEGIN TRAN"), "SQL source leaves transaction ownership with caller");
        Check(source.Contains("sp_getapplock") && source.Contains("Request payload conflict"), "SQL source serializes request IDs and rejects changed payload");
        Check(!source.Contains("SET SoLuongDangDat") && source.Contains("INSERT dbo.GIAODICHKHO"), "SQL source preserves reserved stock and inserts log");
        var store = new MockStock();
        var change = Change();
        using (var tx = new MockTransaction(store))
        {
            var result = service.Apply(tx, change);
            Check(result.Before == 0 && result.After == 3 && !result.Replayed && tx.Commits == 0, "mock ADO: missing stock created on increase without service commit");
            tx.Commit();
        }
        using (var tx = new MockTransaction(store))
        {
            Check(service.Apply(tx, change).Replayed && store.Quantity == 3 && store.Logs.Count == 1, "mock ADO: replay does not apply twice");
            tx.Commit();
        }
        using (var tx = new MockTransaction(store))
        {
            change.Quantity = 4; Check(Reject(() => service.Apply(tx, change)), "mock ADO: same ID changed payload rejected");
        }
        using (var tx = new MockTransaction(store)) Check(Reject(() => service.Apply(tx, Change("DIEU_CHINH_GIAM", 4))) && store.Quantity == 3, "mock ADO: decrease exceeding positive stock rejected");
        using (var tx = new MockTransaction(store))
        {
            Check(service.Apply(tx, Change("DIEU_CHINH_GIAM", 3)).After == 0 && store.Reserved == 8, "mock ADO: decrease to zero preserves reserved");
            tx.Commit();
        }
        using (var tx = new MockTransaction(store)) Check(Reject(() => service.Apply(tx, Change("DIEU_CHINH_GIAM", 1))), "mock ADO: cannot decrease below zero");
        var beforeLogs = store.Logs.Count;
        using (var tx = new MockTransaction(store))
        {
            service.Apply(tx, Change()); tx.Rollback();
            Check(store.Quantity == 0 && store.Logs.Count == beforeLogs, "mock ADO: outer business rollback restores stock and log");
        }
        store.FailLog = true;
        using (var tx = new MockTransaction(store))
        {
            Check(Reject(() => service.Apply(tx, Change())), "mock ADO: log failure propagated");
            tx.Rollback(); Check(store.Quantity == 0 && store.Logs.Count == beforeLogs, "mock ADO: log failure rollback");
        }
        store.FailLog = false; store.Quantity = int.MaxValue;
        using (var tx = new MockTransaction(store)) Check(Reject(() => service.Apply(tx, Change())), "mock ADO: overflow rejected");
        store.Quantity = null;
        using (var tx = new MockTransaction(store)) Check(Reject(() => service.Apply(tx, Change("DIEU_CHINH_GIAM"))), "mock ADO: missing stock decrease rejected");
        foreach (var type in new[] { "NHAP", "BAN", "TRA_HANG" })
        {
            var c = Change(type); if (type == "NHAP") c.ReceiptId = "PN"; else c.OrderId = "DH";
            c.Validate(); Check(true, "business linkage validation " + type);
        }
        var db = new AdjustmentContext(); db.Variants.Add(new BIENSANPHAM { MaBienSanPham = "V", SKU = "SKU" });
        db.Staff.Add(new NHANVIEN { MaNV = "NV", TrangThai = true, TAIKHOAN = new TAIKHOAN { TenDangNhap = "admin", MaVaiTro = "ADMIN", TrangThai = true } });
        using (var c = new MockInventoryController())
        {
            var field = typeof(InventoryController).GetField("db", BindingFlags.Instance | BindingFlags.NonPublic);
            ((IDisposable)field.GetValue(c)).Dispose(); field.SetValue(c, db);
            c.ControllerContext = new ControllerContext { Controller = c, HttpContext = new ActorContext() };
            var form = (InventoryAdjustmentViewModel)((ViewResult)c.Adjust("V")).Model;
            form.Quantity = 2; form.Reason = "checked";
            Check(c.Adjust("V", form) is RedirectToRouteResult && c.Last.StaffId == "NV" && c.Last.VariantId == "V", "controller binds actor/variant server-side and accepts protected token");
            c.ModelState.Clear(); form.OperationToken = "forged";
            Check(c.Adjust("V", form) is ViewResult && !c.ModelState.IsValid, "controller rejects forged token");
            c.ModelState.Clear(); form = (InventoryAdjustmentViewModel)((ViewResult)c.Adjust("V")).Model; form.Quantity = 1; form.Reason = " ";
            Check(c.Adjust("V", form) is ViewResult && !c.ModelState.IsValid, "controller rejects whitespace reason");
            Check(c.Adjust("missing") is HttpNotFoundResult && c.History("missing") is HttpNotFoundResult, "adjust/history missing variant");
            db.Logs.Add(new GIAODICHKHO { MaGiaoDich = 1, MaBienSanPham = "V", LoaiGiaoDich = "DIEU_CHINH_TANG", SoLuong = 2, SoLuongTruoc = 0, SoLuongSau = 2, MaNV = "NV", GhiChu = "reason", NgayGiaoDich = DateTime.Today });
            db.Logs.Add(new GIAODICHKHO { MaGiaoDich = 2, MaBienSanPham = "V", LoaiGiaoDich = "BAN" });
            var history = (System.Collections.Generic.IEnumerable<GIAODICHKHO>)((ViewResult)c.History("V")).Model;
            Check(history.Single().SoLuongSau == 2 && history.Single().MaNV == "NV", "history shows adjustment audit and excludes sales");
            Check(((System.Collections.Generic.IEnumerable<GIAODICHKHO>)((ViewResult)c.History("V", "9999999999999")).Model).Count() == 1, "history handles invalid page");
            var attrs = new System.Collections.Generic.List<System.ComponentModel.DataAnnotations.ValidationResult>();
            form.Quantity = 0; form.Reason = "reason"; form.Type = "BAN";
            Check(!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(form, new System.ComponentModel.DataAnnotations.ValidationContext(form), attrs, true), "form validation restricts positive integer and manual types");
            c.ModelState.Clear(); form = (InventoryAdjustmentViewModel)((ViewResult)c.Adjust("V")).Model;
            form.Quantity = 1; form.Reason = "checked"; db.Staff.Find("NV").TrangThai = false;
            Check(c.Adjust("V", form) is ViewResult && !c.ModelState.IsValid && c.Last.Quantity == 2, "inactive staff mapping cannot invoke writer");
        }
        Console.WriteLine("Completed " + count + " adjustment checks: validation, controller, SQL source and mocked ADO only; SQL Server concurrency/rollback UNVERIFIED.");
    }
}
class AdjustmentContext : FakeContext
{
    public readonly MemorySet<NHANVIEN> Staff = new MemorySet<NHANVIEN>(x => x.MaNV);
    public readonly MemorySet<GIAODICHKHO> Logs = new MemorySet<GIAODICHKHO>(x => x.MaGiaoDich.ToString());
    public override DbSet<NHANVIEN> NHANVIENs { get { return Staff; } set { } }
    public override DbSet<GIAODICHKHO> GIAODICHKHOes { get { return Logs; } set { } }
}
class ActorContext : TestHttpContext
{
    public override IPrincipal User { get { return new GenericPrincipal(new GenericIdentity("admin"), new[] { "ADMIN" }); } set { } }
}
class MockInventoryController : InventoryController
{
    public InventoryChange Last;
    protected override InventoryResult ExecuteChange(InventoryChange change) { change.Validate(); Last = change; return new InventoryResult(); }
}
class MockStock
{
    public int? Quantity;
    public int Reserved = 8;
    public bool FailLog;
    public Dictionary<string, Tuple<string, int, int>> Logs = new Dictionary<string, Tuple<string, int, int>>();
}
class MockTransaction : DbTransaction
{
    public readonly MockStock Store;
    readonly int? original;
    readonly Dictionary<string, Tuple<string, int, int>> logs;
    readonly MockConnection connection;
    bool completed;
    public int Commits;
    public MockTransaction(MockStock store) { Store = store; original = store.Quantity; logs = new Dictionary<string, Tuple<string, int, int>>(store.Logs); connection = new MockConnection(this); }
    public override IsolationLevel IsolationLevel { get { return IsolationLevel.ReadCommitted; } }
    protected override DbConnection DbConnection { get { return connection; } }
    public override void Commit() { Commits++; completed = true; }
    public override void Rollback() { Store.Quantity = original; Store.Logs = new Dictionary<string, Tuple<string, int, int>>(logs); completed = true; }
    protected override void Dispose(bool disposing) { if (!completed) Rollback(); base.Dispose(disposing); }
}
class MockConnection : DbConnection
{
    readonly MockTransaction tx;
    public MockConnection(MockTransaction value) { tx = value; }
    public override string ConnectionString { get; set; }
    public override string Database { get { return "mock"; } }
    public override string DataSource { get { return "mock"; } }
    public override string ServerVersion { get { return "mock"; } }
    public override ConnectionState State { get { return ConnectionState.Open; } }
    public override void ChangeDatabase(string value) { throw new NotSupportedException(); }
    public override void Close() { }
    public override void Open() { }
    protected override DbTransaction BeginDbTransaction(IsolationLevel value) { throw new NotSupportedException(); }
    protected override DbCommand CreateDbCommand() { return new MockCommand(tx); }
}
class MockCommand : DbCommand
{
    readonly MockTransaction tx;
    readonly SqlCommand parameters = new SqlCommand();
    public MockCommand(MockTransaction value) { tx = value; }
    public override string CommandText { get; set; }
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; }
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection DbConnection { get; set; }
    protected override DbTransaction DbTransaction { get; set; }
    protected override DbParameterCollection DbParameterCollection { get { return parameters.Parameters; } }
    protected override DbParameter CreateDbParameter() { return new SqlParameter(); }
    public override void Cancel() { }
    public override void Prepare() { }
    public override int ExecuteNonQuery() { throw new NotSupportedException(); }
    public override object ExecuteScalar() { throw new NotSupportedException(); }
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        if (DbTransaction != tx || !CommandText.Contains("INSERT dbo.GIAODICHKHO")) throw new Exception("wrong transaction/command");
        var p = parameters.Parameters;
        var key = (string)p["@marker"].Value;
        var signature = string.Join("|", p.Cast<SqlParameter>().Where(x => x.ParameterName != "@marker").Select(x => x.Value.ToString()));
        var s = tx.Store; Tuple<string, int, int> prior;
        bool replay = s.Logs.TryGetValue(key, out prior);
        int before, after;
        if (replay)
        {
            if (prior.Item1 != signature) throw new InvalidOperationException("payload conflict"); before = prior.Item2; after = prior.Item3;
        }
        else
        {
            var delta = (int)p["@delta"].Value;
            if (!s.Quantity.HasValue && delta < 0) throw new InvalidOperationException("missing stock");
            before = s.Quantity ?? 0; long next = (long)before + delta;
            if (next < 0 || next > int.MaxValue) throw new InvalidOperationException("bounds");
            after = (int)next; s.Quantity = after;
            if (s.FailLog) throw new InvalidOperationException("log insert failed");
            s.Logs.Add(key, Tuple.Create(signature, before, after));
        }
        var table = new DataTable(); table.Columns.Add("Id", typeof(long)); table.Columns.Add("Before", typeof(int)); table.Columns.Add("After", typeof(int)); table.Columns.Add("Replayed", typeof(bool));
        table.Rows.Add((long)s.Logs.Count, before, after, replay); return table.CreateDataReader();
    }
}

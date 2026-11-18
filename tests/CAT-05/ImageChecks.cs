// Real filesystem/GDI+ and MVC controller calls, with a simulated database; no SQL or Application_Start.
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using QuanLiDienThoaiDD.Areas.Admin;
using QuanLiDienThoaiDD.Areas.Admin.Controllers;
using QuanLiDienThoaiDD.Areas.Admin.ViewModels;
using QuanLiDienThoaiDD.Models;

class ImageChecks
{
    static int checks;
    static byte[] png, jpg;
    static void Check(bool value, string name)
    {
        if (!value) throw new Exception("FAIL: " + name);
        checks++;
        Console.WriteLine("PASS: " + name);
    }
    static byte[] ImageBytes(int width, int height, ImageFormat format)
    {
        using (var image = new Bitmap(width, height))
        using (var bytes = new MemoryStream())
        {
            image.SetPixel(0, 0, Color.Red);
            image.Save(bytes, format);
            return bytes.ToArray();
        }
    }
    static Upload File(byte[] bytes, string name, string mime) { return new Upload(bytes, name, mime); }
    static void Reject(string directory, Upload upload, string name)
    {
        var before = Directory.GetFiles(directory).Length;
        bool rejected = false;
        try { VariantImageStorage.Save(upload, directory); }
        catch (InvalidDataException) { rejected = true; }
        Check(rejected && Directory.GetFiles(directory).Length == before, name);
    }
    static string Physical(string directory, string url) { return Path.Combine(directory, Path.GetFileName(url)); }

    static void Storage(string directory)
    {
        foreach (var tuple in new[] {
            new { Bytes = png, Name = "phone.png", Mime = "image/png" },
            new { Bytes = jpg, Name = "phone.jpg", Mime = "image/jpeg" },
            new { Bytes = jpg, Name = "PHONE.JPEG", Mime = "image/jpeg" },
            new { Bytes = png, Name = "../../outside.png", Mime = "image/png" } })
        {
            var url = VariantImageStorage.Save(File(tuple.Bytes, tuple.Name, tuple.Mime), directory);
            Check(System.Text.RegularExpressions.Regex.IsMatch(url, @"\A/images/variants/[a-f0-9]{32}\.png\z"), "server-generated storefront URL: " + tuple.Name);
            using (var image = Image.FromFile(Physical(directory, url)))
                Check(image.Width == 3 && image.Height == 2 && image.RawFormat.Guid == ImageFormat.Png.Guid, "saved decoded PNG: " + tuple.Name);
        }
        Reject(directory, File(png, "photo.aspx", "image/png"), "reject executable extension");
        Reject(directory, File(png, "photo.svg", "image/svg+xml"), "reject SVG");
        Reject(directory, File(png, "photo.png", "text/plain"), "reject incorrect MIME");
        Reject(directory, File(png, "photo.png", null), "reject missing MIME");
        Reject(directory, File(png, "photo.jpg", "image/jpeg"), "reject PNG disguised as JPEG");
        Reject(directory, File(jpg, "photo.png", "image/png"), "reject JPEG disguised as PNG");
        Reject(directory, File(Encoding.UTF8.GetBytes("<script>alert(1)</script>"), "photo.png", "image/png"), "reject fake content");
        Reject(directory, File(png.Take(24).ToArray(), "photo.png", "image/png"), "reject truncated PNG");
        Reject(directory, File(new byte[0], "photo.png", "image/png"), "reject empty image");
        Reject(directory, File(new byte[VariantImageStorage.MaxBytes + 1], "photo.png", "image/png"), "reject advertised oversize");
        var lying = File(new byte[VariantImageStorage.MaxBytes + 1], "photo.png", "image/png");
        lying.Length = 100;
        Reject(directory, lying, "bounded read rejects actual oversize with false ContentLength");
        lying = File(png, "photo.png", "image/png"); lying.Length++;
        Reject(directory, lying, "reject incomplete content");
        Reject(directory, File(ImageBytes(4097, 1, ImageFormat.Png), "wide.png", "image/png"), "reject excessive width");
        Reject(directory, File(ImageBytes(1, 4097, ImageFormat.Png), "tall.png", "image/png"), "reject excessive height");
        Reject(directory, File(ImageBytes(4001, 4000, ImageFormat.Png), "pixels.png", "image/png"), "reject excessive pixels");
        var boundary = new byte[VariantImageStorage.MaxBytes]; Array.Copy(png, boundary, png.Length);
        var paddedUrl = VariantImageStorage.Save(File(boundary, "boundary.png", "image/png"), directory);
        Check(new FileInfo(Physical(directory, paddedUrl)).Length < 1024, "exact 2 MiB accepted, trailing bytes stripped");
        var payload = png.Concat(Encoding.UTF8.GetBytes("<%@ Page Language=\"C#\" %>UNSAFE_PAYLOAD")).ToArray();
        var cleanUrl = VariantImageStorage.Save(File(payload, "payload.png", "image/png"), directory);
        Check(!Encoding.UTF8.GetString(System.IO.File.ReadAllBytes(Physical(directory, cleanUrl))).Contains("UNSAFE_PAYLOAD"), "re-encode removes appended payload");
        var guard = Path.Combine(directory, "same.png"); System.IO.File.WriteAllText(guard, "existing file");
        var urls = new string[12];
        Parallel.For(0, urls.Length, i => urls[i] = VariantImageStorage.Save(File(png, "same.png", "image/png"), directory));
        Check(urls.Distinct().Count() == urls.Length && urls.All(x => System.IO.File.Exists(Physical(directory, x)))
            && System.IO.File.ReadAllText(guard) == "existing file", "parallel same client filename does not overwrite");
        var unreferenced = urls[0];
        VariantImageStorage.TryDiscard(unreferenced, directory, x => false);
        Check(!System.IO.File.Exists(Physical(directory, unreferenced)), "discard unreferenced new upload");
        VariantImageStorage.TryDiscard(urls[1], directory, x => true);
        Check(System.IO.File.Exists(Physical(directory, urls[1])), "retain referenced upload");
        VariantImageStorage.TryDiscard(urls[2], directory, x => { throw new IOException("DB unavailable"); });
        Check(System.IO.File.Exists(Physical(directory, urls[2])), "retain upload if reference query fails");
        using (var locked = new FileStream(Physical(directory, urls[3]), FileMode.Open, FileAccess.Read, FileShare.None))
        {
            VariantImageStorage.TryDiscard(urls[3], directory, x => false);
            Check(System.IO.File.Exists(Physical(directory, urls[3])), "cleanup IO failure is safe and retains file");
        }
        VariantImageStorage.TryDiscard("/images/variants/../../same.png", directory, x => false);
        VariantImageStorage.TryDiscard("/images/same.png", directory, x => false);
        Check(System.IO.File.Exists(guard), "cleanup never deletes legacy or traversal paths");
        bool ioFailure = false;
        try { VariantImageStorage.Save(File(png, "photo.png", "image/png"), guard); }
        catch (IOException) { ioFailure = true; }
        Check(ioFailure && System.IO.File.ReadAllText(guard) == "existing file", "real filesystem save failure");
    }

    static ProductVariantsController Controller(ImageContext db, string directory)
    {
        var controller = new ProductVariantsController();
        var field = typeof(ProductVariantsController).GetField("db", BindingFlags.Instance | BindingFlags.NonPublic);
        ((IDisposable)field.GetValue(controller)).Dispose();
        field.SetValue(controller, db);
        controller.ControllerContext = new ControllerContext { Controller = controller, HttpContext = new TestContext(directory) };
        return controller;
    }
    static ImageContext Seed()
    {
        var db = new ImageContext();
        db.Products.Add(new SANPHAM { MaSanPham = "P", TenSanPham = "Phone", TrangThai = true });
        db.Variants.Add(new BIENSANPHAM { MaBienSanPham = "V", MaSanPham = "P", SKU = "OLD", HinhAnhUrl = "/images/shared.png" });
        db.Variants.Add(new BIENSANPHAM { MaBienSanPham = "OTHER", MaSanPham = "P", SKU = "OTHER", HinhAnhUrl = "/images/shared.png" });
        return db;
    }
    static ProductVariantEditViewModel Edit(bool upload)
    {
        return new ProductVariantEditViewModel { SKU = "EDITED", GiaNhap = 1, GiaBan = 2, HinhAnhUrl = "/images/shared.png",
            ImageUpload = upload ? File(png, "phone.png", "image/png") : null, TrangThai = true };
    }
    static ProductVariantViewModel Create(bool upload)
    {
        return new ProductVariantViewModel { MaBienSanPham = "NEW", SKU = "NEW", GiaNhap = 1, GiaBan = 2,
            ImageUpload = upload ? File(png, "phone.png", "image/png") : null, TrangThai = true };
    }
    static void Controllers(string root)
    {
        var oldPath = Path.Combine(root, "shared.png"); System.IO.File.WriteAllBytes(oldPath, png);
        var folder = Path.Combine(root, "controller"); Directory.CreateDirectory(folder);
        using (var db = Seed()) using (var c = Controller(db, folder))
        {
            var get = (ProductVariantEditViewModel)((ViewResult)c.Edit("P", "V")).Model;
            Check(get.HinhAnhUrl == "/images/shared.png" && (string)c.ViewBag.CurrentImageUrl == get.HinhAnhUrl, "GET exposes current DB image");
            Check(c.Create("P", Create(true)) is RedirectToRouteResult && db.Saves == 1, "create uploads and saves");
            var createdUrl = db.Variants.Find("NEW").HinhAnhUrl;
            Check(System.IO.File.Exists(Physical(folder, createdUrl)), "created URL points to real file");
            Check(c.Edit("P", "V", Edit(true)) is RedirectToRouteResult && db.Saves == 2, "edit replaces image");
            Check(System.IO.File.Exists(oldPath) && db.Variants.Find("OTHER").HinhAnhUrl == "/images/shared.png", "replace preserves shared old image");
            var newUrl = db.Variants.Find("V").HinhAnhUrl;
            var edit = Edit(false); edit.HinhAnhUrl = newUrl;
            Check(c.Edit("P", "V", edit) is RedirectToRouteResult && db.Variants.Find("V").HinhAnhUrl == newUrl, "no upload preserves current path");
            edit = Edit(false); edit.RemoveImage = true;
            Check(c.Edit("P", "V", edit) is RedirectToRouteResult && db.Variants.Find("V").HinhAnhUrl == null
                && System.IO.File.Exists(Physical(folder, newUrl)), "remove clears reference and retains old file for sessions");
            edit = Edit(true); edit.RemoveImage = true;
            var count = Directory.GetFiles(folder).Length; var saves = db.Saves;
            Check(c.Edit("P", "V", edit) is ViewResult && c.ModelState["ImageUpload"].Errors.Count > 0 && db.Saves == saves
                && Directory.GetFiles(folder).Length == count, "reject upload and remove together without side effects");
            c.ModelState.Clear(); edit = Edit(true); edit.ImageUpload = File(new byte[] { 1, 2, 3 }, "fake.png", "image/png");
            Check(c.Edit("P", "V", edit) is ViewResult && c.ModelState["ImageUpload"].Errors.Count > 0 && db.Saves == saves, "invalid content returns field error without DB save");
            c.ModelState.Clear(); c.ModelState.AddModelError("SKU", "fixture invalid form");
            Check(c.Edit("P", "V", Edit(true)) is ViewResult && Directory.GetFiles(folder).Length == count, "other validation failure writes no image");
        }
        using (var db = Seed()) using (var c = Controller(db, oldPath))
        {
            Check(c.Create("P", Create(true)) is ViewResult && db.Saves == 0 && c.ModelState["ImageUpload"].Errors.Count > 0,
                "filesystem write failure does not save database or leak physical path");
            Check(!c.ModelState["ImageUpload"].Errors[0].ErrorMessage.Contains(root), "write error hides physical path");
        }
        foreach (var creating in new[] { false, true })
        foreach (var mode in new[] { "rollback", "commit-then-error", "reference-query-error", "brand-reference", "concurrency", "unexpected-save-error", "unknown-db-error" })
        {
            var directory = Path.Combine(root, (creating ? "create-" : "edit-") + mode); Directory.CreateDirectory(directory);
            using (var db = Seed()) using (var c = Controller(db, directory))
            {
                db.Mode = mode;
                var result = creating ? c.Create("P", Create(true)) : c.Edit("P", "V", Edit(true));
                Check(result is ViewResult && c.ModelState[""].Errors.Count > 0, (creating ? "create " : "edit ") + mode + " returns save error");
                bool shouldRetain = mode == "commit-then-error" || mode == "reference-query-error" || mode == "brand-reference"
                    || mode == "unexpected-save-error" || mode == "unknown-db-error";
                Check(Directory.GetFiles(directory).Length == (shouldRetain ? 1 : 0), (creating ? "create " : "edit ") + mode + " safe compensation");
                Check(System.IO.File.Exists(oldPath), mode + " preserves old file");
                Check(!c.ModelState[""].Errors.Any(x => x.ErrorMessage.Contains("secret fixture")), mode + " hides save exception detail");
            }
        }
    }
    public static void Main(string[] args)
    {
        png = ImageBytes(3, 2, ImageFormat.Png); jpg = ImageBytes(3, 2, ImageFormat.Jpeg);
        // Unique test-owned output stays in ignored obj; no app images/data are modified or deleted.
        var root = Path.Combine(Path.GetFullPath(args[0]), "files-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        var storage = Path.Combine(root, "storage"); Directory.CreateDirectory(storage);
        Storage(storage); Controllers(root);
        Console.WriteLine("Completed " + checks + " image checks (real files + simulated DB). IIS/SQL/UI acceptance remains UNVERIFIED.");
    }
}

class Upload : HttpPostedFileBase
{
    readonly MemoryStream stream;
    readonly string name, mime;
    public int Length;
    public Upload(byte[] bytes, string fileName, string contentType) { stream = new MemoryStream(bytes); Length = bytes.Length; name = fileName; mime = contentType; }
    public override int ContentLength { get { return Length; } }
    public override string FileName { get { return name; } }
    public override string ContentType { get { return mime; } }
    public override Stream InputStream { get { return stream; } }
}
class TestContext : HttpContextBase
{
    readonly HttpServerUtilityBase server;
    public TestContext(string directory) { server = new TestServer(directory); }
    public override HttpServerUtilityBase Server { get { return server; } }
}
class TestServer : HttpServerUtilityBase
{
    readonly string directory;
    public TestServer(string value) { directory = value; }
    public override string MapPath(string path)
    {
        if (path != "~/images/variants/") throw new Exception("Unexpected storage path");
        return directory;
    }
}
class ImageContext : QLHANGHOA_PHUKIEN_WEBEntities
{
    public readonly Rows<SANPHAM> Products = new Rows<SANPHAM>(x => x.MaSanPham);
    public readonly Rows<BIENSANPHAM> Variants = new Rows<BIENSANPHAM>(x => x.MaBienSanPham);
    public readonly Rows<THUONGHIEU> Brands = new Rows<THUONGHIEU>(x => x.MaThuongHieu);
    public int Saves;
    public string Mode;
    public override DbSet<SANPHAM> SANPHAMs { get { return Products; } set { } }
    public override DbSet<BIENSANPHAM> BIENSANPHAMs { get { return Variants; } set { } }
    public override DbSet<THUONGHIEU> THUONGHIEUx { get { return Brands; } set { } }
    public override int SaveChanges()
    {
        if (Mode == null) { Saves++; return 1; }
        // Simulate the persisted reference state after SaveChanges throws; this is not SQL transaction evidence.
        if (Mode != "commit-then-error")
        {
            var changed = Variants.Find("NEW") ?? Variants.Find("V");
            if (Mode == "brand-reference") Brands.Add(new THUONGHIEU { MaThuongHieu = "B", LogoUrl = "~" + changed.HinhAnhUrl });
            changed.HinhAnhUrl = changed.MaBienSanPham == "V" ? "/images/shared.png" : null;
        }
        if (Mode == "reference-query-error") Variants.QueryError = true;
        if (Mode == "concurrency") throw new DbUpdateConcurrencyException("secret fixture");
        if (Mode == "unexpected-save-error") throw new InvalidOperationException("secret fixture");
        if (Mode == "rollback" || Mode == "reference-query-error" || Mode == "brand-reference")
            throw new DbEntityValidationException("secret fixture");
        throw new DbUpdateException("secret fixture");
    }
}
class Rows<T> : DbSet<T>, IQueryable<T> where T : class
{
    readonly List<T> rows = new List<T>();
    readonly Func<T, string> key;
    public bool QueryError;
    public Rows(Func<T, string> selector) { key = selector; }
    IQueryable<T> Query { get { if (QueryError) throw new InvalidOperationException("reference query unavailable"); return rows.AsQueryable(); } }
    public override T Add(T entity) { rows.Add(entity); return entity; }
    public override T Find(params object[] keys) { return rows.SingleOrDefault(x => key(x) == (string)keys[0]); }
    Type IQueryable.ElementType { get { return Query.ElementType; } }
    Expression IQueryable.Expression { get { return Query.Expression; } }
    IQueryProvider IQueryable.Provider { get { return Query.Provider; } }
    public IEnumerator<T> GetEnumerator() { return rows.GetEnumerator(); }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return rows.GetEnumerator(); }
}

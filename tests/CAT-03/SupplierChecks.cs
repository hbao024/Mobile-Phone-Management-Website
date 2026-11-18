// Database-free checks against the built application. No Application_Start or SQL is executed.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.CodeDom.Compiler;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Razor;
using System.Web.Razor;
using Microsoft.CSharp;
using QuanLiDienThoaiDD.Areas.Admin.Controllers;
using QuanLiDienThoaiDD.Areas.Admin.ViewModels;
using QuanLiDienThoaiDD.Models;

class SupplierChecks
{
    static int checks;
    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        checks++;
        Console.WriteLine("PASS: " + name);
    }

    static T Bind<T>(SuppliersController controller, params string[] pairs) where T : class
    {
        controller.ModelState.Clear();
        var values = new NameValueCollection();
        for (int i = 0; i < pairs.Length; i += 2) values.Add(pairs[i], pairs[i + 1]);
        var binding = new ModelBindingContext
        {
            ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(null, typeof(T)),
            ModelState = controller.ModelState,
            ValueProvider = new NameValueCollectionValueProvider(values, CultureInfo.InvariantCulture)
        };
        return (T)new DefaultModelBinder().BindModel(controller.ControllerContext, binding);
    }

    static SuppliersController Controller(FakeContext context)
    {
        var controller = new SuppliersController();
        var field = typeof(SuppliersController).GetField("db", BindingFlags.Instance | BindingFlags.NonPublic);
        ((IDisposable)field.GetValue(controller)).Dispose();
        // Keep the production controller constructor unchanged; replace its disconnected context only in this fixture.
        field.SetValue(controller, context);
        controller.ControllerContext = new ControllerContext { Controller = controller, HttpContext = new TestHttpContext() };
        return controller;
    }

    static void CompileViews(string project)
    {
        foreach (var file in Directory.GetFiles(Path.Combine(project, "Areas/Admin/Views/Suppliers"), "*.cshtml"))
        {
            var host = new MvcWebPageRazorHost("~/Areas/Admin/Views/Suppliers/" + Path.GetFileName(file), file);
            foreach (var ns in new[] { "System", "System.Linq", "System.Collections.Generic", "System.Web.Mvc", "System.Web.Mvc.Html" })
                host.NamespaceImports.Add(ns);
            using (var reader = File.OpenText(file))
            using (var compiler = new CSharpCodeProvider())
            {
                var generated = new RazorTemplateEngine(host).GenerateCode(reader);
                Check(generated.Success, "Razor parse " + Path.GetFileName(file));
                var options = new CompilerParameters { GenerateInMemory = true };
                foreach (var assembly in new[] { "System.dll", "System.Core.dll", "Microsoft.CSharp.dll", "System.Web.dll", "System.ComponentModel.DataAnnotations.dll" })
                    options.ReferencedAssemblies.Add(assembly);
                foreach (var assembly in new[] { "QuanLiDienThoaiDD.dll", "System.Web.Mvc.dll", "System.Web.WebPages.dll", "System.Web.WebPages.Razor.dll", "System.Web.Razor.dll", "System.Web.Helpers.dll" })
                    options.ReferencedAssemblies.Add(Path.Combine(project, "bin", assembly));
                var result = compiler.CompileAssemblyFromDom(options, generated.GeneratedCode);
                if (result.Errors.HasErrors)
                    throw new Exception(string.Join(Environment.NewLine, result.Errors.Cast<CompilerError>().Select(x => x.ToString())));
                Check(!result.Errors.HasErrors, "Razor compile " + Path.GetFileName(file));
            }
        }
    }

    public static void Main(string[] args)
    {
        var longEmail = new string('a', 64) + "@" + new string('b', 63) + "." + new string('c', 63) + "." + new string('d', 58) + ".com";
        Check(longEmail.Length == 255, "email boundary fixture length");
        var invalid = new[] {
            new[] { "MaNCC", "" }, new[] { "MaNCC", "   " }, new[] { "MaNCC", "ĐT/01" }, new[] { "MaNCC", new string('A', 11) },
            new[] { "TenNCC", "" }, new[] { "TenNCC", "   " }, new[] { "TenNCC", new string('N', 151) },
            new[] { "DiaChi", new string('D', 301) },
            new[] { "DienThoai", new string('1', 16) }, new[] { "DienThoai", "abc" }, new[] { "DienThoai", "+" }, new[] { "DienThoai", "１２３４５６" },
            new[] { "Email", longEmail + "x" }, new[] { "Email", "not-an-email" }, new[] { "Email", "a@@example.com" }, new[] { "Email", "a b@example.com" },
            new[] { "Email", "a\n@example.com" }, new[] { "Email", "tên@example.com" }
        };
        foreach (var pair in invalid)
        {
            var invalidDb = new FakeContext();
            var row = new NHACUNGCAP { MaNCC = "OLD", TenNCC = "Before", TrangThai = true };
            invalidDb.Rows.Add(row);
            using (var controller = Controller(invalidDb))
            {
                var values = new List<string> { "TrangThai", "true" };
                if (pair[0] != "MaNCC") values.AddRange(new[] { "MaNCC", "NEW" });
                if (pair[0] != "TenNCC") values.AddRange(new[] { "TenNCC", "New" });
                values.AddRange(pair);
                var create = Bind<SupplierViewModel>(controller, values.ToArray());
                Check(controller.Create(create) is ViewResult && controller.ModelState[pair[0]].Errors.Count > 0
                    && invalidDb.Saves == 0 && invalidDb.Rows.Count() == 1, "invalid create " + pair[0] + " length " + pair[1].Length);
                if (pair[0] == "MaNCC") continue;
                var edit = Bind<SupplierEditViewModel>(controller, values.ToArray());
                Check(controller.Edit("OLD", edit) is ViewResult && controller.ModelState[pair[0]].Errors.Count > 0
                    && invalidDb.Saves == 0 && row.TenNCC == "Before" && row.TrangThai, "invalid edit " + pair[0] + " length " + pair[1].Length);
            }
        }

        var db = new FakeContext();
        var existing = new NHACUNGCAP { MaNCC = "OLD", TenNCC = "Existing", DienThoai = "0901234567", Email = "same@example.com", TrangThai = false };
        existing.SANPHAMs.Add(new SANPHAM { MaSanPham = "KEEP", MaNCCMacDinh = "OLD" });
        existing.PHIEUNHAPs.Add(new PHIEUNHAP { MaPhieuNhap = "RECEIPT", MaNCC = "OLD" });
        db.Rows.Add(existing);
        using (var controller = Controller(db))
        {
            Check(((SupplierViewModel)((ViewResult)controller.Create()).Model).TrangThai, "create defaults active");
            var model = Bind<SupplierViewModel>(controller, "MaNCC", "OLD", "TenNCC", "New");
            Check(controller.Create(model) is ViewResult && controller.ModelState["MaNCC"].Errors.Count > 0 && db.Saves == 0, "duplicate inactive code");

            model = Bind<SupplierViewModel>(controller, "MaNCC", "A_123456-0", "TenNCC", new string('N', 150),
                "DiaChi", new string('D', 300), "DienThoai", new string('1', 15), "Email", longEmail, "TrangThai", "true",
                "SANPHAMs[0].MaSanPham", "HACK", "PHIEUNHAPs[0].MaPhieuNhap", "HACK");
            Check(controller.Create(model) is RedirectToRouteResult && db.Saves == 1, "create at all five SQL length boundaries");
            var added = db.Rows.Find("A_123456-0");
            Check(added.TenNCC.Length == 150 && added.DiaChi.Length == 300 && added.DienThoai.Length == 15 && added.Email == longEmail
                && added.TrangThai && added.SANPHAMs.Count == 0 && added.PHIEUNHAPs.Count == 0, "create maps scalar fields only");
            Check(controller.TempData["SupplierSuccess"] != null, "create success message");

            model = Bind<SupplierViewModel>(controller, "MaNCC", "DUP", "TenNCC", " Existing ", "DienThoai", existing.DienThoai, "Email", existing.Email, "TrangThai", "false");
            Check(controller.Create(model) is RedirectToRouteResult && db.Saves == 2 && db.Rows.Find("DUP").TenNCC == "Existing",
                "duplicate name phone email accepted; trim name; create inactive");
            model = Bind<SupplierViewModel>(controller, "MaNCC", "EMPTY", "TenNCC", "Optional", "DiaChi", "  ", "DienThoai", "", "Email", "");
            Check(controller.Create(model) is RedirectToRouteResult && db.Saves == 3 && db.Rows.Find("EMPTY").DiaChi == null
                && db.Rows.Find("EMPTY").DienThoai == null && db.Rows.Find("EMPTY").Email == null, "optional fields blank become null");

            Check(controller.Edit("missing") is HttpNotFoundResult && controller.Edit(null) is HttpNotFoundResult, "GET missing or absent id");
            var edit = Bind<SupplierEditViewModel>(controller, "TenNCC", "Name");
            Check(controller.Edit("missing", edit) is HttpNotFoundResult && controller.Edit(null, edit) is HttpNotFoundResult && db.Saves == 3, "POST missing or absent id does not save");
            edit = Bind<SupplierEditViewModel>(controller, "TenNCC", "Existing", "DiaChi", " Updated ", "DienThoai", "(028) 123-4567",
                "Email", "same@example.com", "TrangThai", "true", "MaNCC", "HACK",
                "SANPHAMs[0].MaSanPham", "HACK", "PHIEUNHAPs[0].MaPhieuNhap", "HACK");
            Check(controller.Edit("OLD", edit) is RedirectToRouteResult && existing.TrangThai && existing.TenNCC == "Existing"
                && existing.DiaChi == "Updated" && existing.DienThoai == "(028) 123-4567" && existing.Email == "same@example.com", "edit all contact fields and activate; duplicates allowed");
            Check(existing.MaNCC == "OLD" && existing.SANPHAMs.Single().MaSanPham == "KEEP" && existing.SANPHAMs.Single().MaNCCMacDinh == "OLD"
                && existing.PHIEUNHAPs.Single().MaPhieuNhap == "RECEIPT" && existing.PHIEUNHAPs.Single().MaNCC == "OLD" && db.Rows.Find("HACK") == null,
                "edit cannot overpost key products or receipts");
            Check(controller.TempData["SupplierSuccess"] != null, "edit success message");
            var loaded = (SupplierEditViewModel)((ViewResult)controller.Edit("OLD")).Model;
            Check(loaded.TenNCC == existing.TenNCC && loaded.DiaChi == existing.DiaChi && loaded.DienThoai == existing.DienThoai
                && loaded.Email == existing.Email && loaded.TrangThai, "GET edit returns all scalar fields");
            edit = Bind<SupplierEditViewModel>(controller, "TenNCC", new string('N', 150), "DiaChi", new string('D', 300),
                "DienThoai", new string('1', 15), "Email", longEmail, "TrangThai", "false");
            Check(controller.Edit("OLD", edit) is RedirectToRouteResult && !existing.TrangThai && existing.TenNCC.Length == 150
                && existing.DiaChi.Length == 300 && existing.Email.Length == 255, "edit boundaries; duplicate name and deactivate");
            edit = Bind<SupplierEditViewModel>(controller, "TenNCC", "Existing", "TrangThai", "false");
            Check(controller.Edit("OLD", edit) is RedirectToRouteResult && !existing.TrangThai && existing.DiaChi == null && existing.DienThoai == null && existing.Email == null
                && existing.SANPHAMs.Count == 1 && existing.PHIEUNHAPs.Count == 1, "clear contacts and retain relationships when inactive");
            var list = (IEnumerable<SupplierViewModel>)((ViewResult)controller.Index()).Model;
            Check(list.Count() == 4 && list.Any(x => x.TrangThai) && list.Any(x => !x.TrangThai), "list both statuses");
            Check(list.Single(x => x.MaNCC == "A_123456-0").Email == longEmail && list.Single(x => x.MaNCC == "DUP").DienThoai == "0901234567",
                "list contains contact fields");

            db.SaveError = new DbUpdateException("fixture save failure; SQL detail must not leak");
            edit = Bind<SupplierEditViewModel>(controller, "TenNCC", "Changed", "TrangThai", "false");
            Check(controller.Edit("OLD", edit) is ViewResult && controller.ModelState[""].Errors.Count > 0
                && !controller.ModelState[""].Errors[0].ErrorMessage.Contains("SQL detail"), "edit save failure returns safe message");
            model = Bind<SupplierViewModel>(controller, "MaNCC", "FAIL", "TenNCC", "Failure");
            Check(controller.Create(model) is ViewResult && controller.ModelState[""].Errors.Count > 0, "create save failure returns form");
            db.SaveError = new DbUpdateConcurrencyException("fixture missing row");
            edit = Bind<SupplierEditViewModel>(controller, "TenNCC", "Changed");
            Check(controller.Edit("OLD", edit) is ViewResult && controller.ModelState[""].Errors.Count > 0, "row removed during save");
        }

        var auth = (AuthorizeAttribute)Attribute.GetCustomAttribute(typeof(SuppliersController), typeof(AuthorizeAttribute));
        Check(auth != null && auth.Roles == "ADMIN", "ADMIN metadata");
        var posts = typeof(SuppliersController).GetMethods().Where(x => Attribute.IsDefined(x, typeof(HttpPostAttribute))).ToArray();
        Check(posts.Length == 2 && posts.All(x => Attribute.IsDefined(x, typeof(ValidateAntiForgeryTokenAttribute))), "both writes require POST and anti-forgery");
        Check(!typeof(SuppliersController).GetMethods().Any(x => x.Name == "Delete"), "no hard delete action");
        CompileViews(args[0]);
        Console.WriteLine("Completed " + checks + " database-free checks. SQL persistence, collation, HTTP authorization and concurrency are NOT verified.");
    }
}
class TestHttpContext : HttpContextBase
{
    readonly System.Collections.IDictionary items = new System.Collections.Hashtable();
    public override System.Collections.IDictionary Items { get { return items; } }
}

class FakeContext : QLHANGHOA_PHUKIEN_WEBEntities
{
    public readonly MemorySuppliers Rows = new MemorySuppliers();
    public int Saves;
    public Exception SaveError;
    public override DbSet<NHACUNGCAP> NHACUNGCAPs { get { return Rows; } set { } }
    public override int SaveChanges()
    {
        if (SaveError != null) throw SaveError;
        Saves++;
        return 1;
    }
}

class MemorySuppliers : DbSet<NHACUNGCAP>, IQueryable<NHACUNGCAP>
{
    readonly List<NHACUNGCAP> rows = new List<NHACUNGCAP>();
    IQueryable<NHACUNGCAP> Query { get { return rows.AsQueryable(); } }
    public override NHACUNGCAP Add(NHACUNGCAP entity) { rows.Add(entity); return entity; }
    public override NHACUNGCAP Find(params object[] keys) { return rows.SingleOrDefault(x => x.MaNCC == (string)keys[0]); }
    Type IQueryable.ElementType { get { return Query.ElementType; } }
    Expression IQueryable.Expression { get { return Query.Expression; } }
    IQueryProvider IQueryable.Provider { get { return Query.Provider; } }
    public IEnumerator<NHACUNGCAP> GetEnumerator() { return rows.GetEnumerator(); }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return rows.GetEnumerator(); }
}

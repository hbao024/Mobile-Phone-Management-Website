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

class BrandChecks
{
    static int checks;
    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        checks++;
        Console.WriteLine("PASS: " + name);
    }

    static T Bind<T>(BrandsController controller, params string[] pairs) where T : class
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

    static BrandsController Controller(FakeContext context)
    {
        var controller = new BrandsController();
        var field = typeof(BrandsController).GetField("db", BindingFlags.Instance | BindingFlags.NonPublic);
        ((IDisposable)field.GetValue(controller)).Dispose();
        // Keep the production controller constructor unchanged; replace its disconnected context only in this fixture.
        field.SetValue(controller, context);
        controller.ControllerContext = new ControllerContext { Controller = controller, HttpContext = new TestHttpContext() };
        return controller;
    }

    static void InvalidCreate(string label, string code, string name, string description, string errorKey, string logo = "")
    {
        var db = new FakeContext();
        using (var controller = Controller(db))
        {
            var model = Bind<BrandViewModel>(controller, "MaThuongHieu", code, "TenThuongHieu", name, "MoTa", description, "LogoUrl", logo);
            Check(controller.Create(model) is ViewResult && controller.ModelState[errorKey].Errors.Count > 0
                && db.Saves == 0 && db.Rows.Count() == 0, label);
        }
    }

    static void CompileViews(string project)
    {
        foreach (var file in Directory.GetFiles(Path.Combine(project, "Areas/Admin/Views/Brands"), "*.cshtml"))
        {
            var host = new MvcWebPageRazorHost("~/Areas/Admin/Views/Brands/" + Path.GetFileName(file), file);
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
        InvalidCreate("missing name", "NEW", "", "", "TenThuongHieu");
        InvalidCreate("whitespace name", "NEW", "   ", "", "TenThuongHieu");
        InvalidCreate("missing code", "", "New", "", "MaThuongHieu");
        InvalidCreate("invalid code characters", "ĐT/01", "New", "", "MaThuongHieu");
        InvalidCreate("code longer than 10", new string('A', 11), "New", "", "MaThuongHieu");
        InvalidCreate("name longer than 100", "NEW", new string('A', 101), "", "TenThuongHieu");
        InvalidCreate("description longer than 500", "NEW", "New", new string('A', 501), "MoTa");
        InvalidCreate("logo longer than 500", "NEW", "New", "", "LogoUrl", "/" + new string('a', 500));
        foreach (var logo in new[] { "javascript:alert(1)", "data:image/png;base64,AAAA", "file:///c:/logo.png", "ftp://example.com/logo.png",
            "//example.com/logo.png", "/\\example.com/logo.png", "/../logo.png", "~/%2e%2e/logo.png", "/%2fexample.com/logo.png",
            "https://user:password@example.com/logo.png", "https://", "images/logo.png", "/images/my logo.png", "/images/\nlogo.png", "/images/%00logo.png" })
            InvalidCreate("invalid logo: " + logo.Replace('\n', ' '), "NEW", "New", "", "LogoUrl", logo);

        foreach (var logo in new[] { "", "   ", "/images/logo.png", "~/images/logo.png", "https://example.com/logo.png?v=1", "http://example.com/logo.png", "/" + new string('a', 499) })
        {
            var logoDb = new FakeContext();
            using (var controller = Controller(logoDb))
            {
                var model = Bind<BrandViewModel>(controller, "MaThuongHieu", "LOGO", "TenThuongHieu", "Logo", "LogoUrl", logo);
                Check(controller.Create(model) is RedirectToRouteResult && logoDb.Saves == 1
                    && logoDb.Rows.Find("LOGO").LogoUrl == (string.IsNullOrWhiteSpace(logo) ? null : logo), "valid optional logo path length " + logo.Length);
            }
        }

        var db = new FakeContext();
        var existing = new THUONGHIEU { MaThuongHieu = "OLD", TenThuongHieu = "Existing", TrangThai = false };
        existing.SANPHAMs.Add(new SANPHAM { MaSanPham = "KEEP", MaThuongHieu = "OLD" });
        db.Rows.Add(existing);
        using (var controller = Controller(db))
        {
            var model = Bind<BrandViewModel>(controller, "MaThuongHieu", "OLD", "TenThuongHieu", "Different");
            Check(controller.Create(model) is ViewResult && controller.ModelState["MaThuongHieu"].Errors.Count > 0 && db.Saves == 0, "duplicate code rejected");
            model = Bind<BrandViewModel>(controller, "MaThuongHieu", "NEW", "TenThuongHieu", " Existing ");
            Check(controller.Create(model) is ViewResult && controller.ModelState["TenThuongHieu"].Errors.Count > 0 && db.Saves == 0, "duplicate inactive name rejected after trim");

            model = Bind<BrandViewModel>(controller, "MaThuongHieu", "A_123456-0", "TenThuongHieu", new string('N', 100), "MoTa", new string('D', 500), "TrangThai", "true");
            Check(controller.Create(model) is RedirectToRouteResult && db.Saves == 1 && db.Rows.Find(model.MaThuongHieu).TrangThai, "create at all length boundaries");
            Check(controller.TempData["BrandSuccess"] != null, "create success message");
            Check(((BrandViewModel)((ViewResult)controller.Create()).Model).TrangThai, "new brand defaults to active");
            Check(controller.Edit("missing") is HttpNotFoundResult && controller.Edit(null) is HttpNotFoundResult, "GET edit missing record");
            var edit = Bind<BrandEditViewModel>(controller, "TenThuongHieu", "Existing", "TrangThai", "true");
            Check(controller.Edit("missing", edit) is HttpNotFoundResult && db.Saves == 1, "POST edit missing record does not save");
            edit = Bind<BrandEditViewModel>(controller, "TenThuongHieu", new string('N', 100), "TrangThai", "true");
            Check(controller.Edit("OLD", edit) is ViewResult && controller.ModelState["TenThuongHieu"].Errors.Count > 0 && db.Saves == 1, "edit duplicate name rejected");
            edit = Bind<BrandEditViewModel>(controller, "TenThuongHieu", "", "TrangThai", "true");
            Check(controller.Edit("OLD", edit) is ViewResult && controller.ModelState["TenThuongHieu"].Errors.Count > 0 && db.Saves == 1, "edit missing name does not save");

            foreach (var pair in new[] { new[] { "TenThuongHieu", new string('N', 101) }, new[] { "MoTa", new string('D', 501) },
                new[] { "LogoUrl", "/" + new string('a', 500) }, new[] { "LogoUrl", "javascript:alert(1)" } })
            {
                edit = pair[0] == "TenThuongHieu"
                    ? Bind<BrandEditViewModel>(controller, pair[0], pair[1])
                    : Bind<BrandEditViewModel>(controller, "TenThuongHieu", "Existing", pair[0], pair[1]);
                Check(controller.Edit("OLD", edit) is ViewResult && controller.ModelState[pair[0]].Errors.Count > 0 && db.Saves == 1, "edit rejects invalid " + pair[0]);
            }

            edit = Bind<BrandEditViewModel>(controller, "TenThuongHieu", "Existing", "MoTa", " Updated ", "LogoUrl", " ~/images/logo.png ", "TrangThai", "true", "MaThuongHieu", "HACK", "SANPHAMs[0].MaSanPham", "HACK");
            Check(controller.Edit("OLD", edit) is RedirectToRouteResult && existing.TrangThai && existing.MoTa == "Updated" && existing.MaThuongHieu == "OLD"
                && existing.LogoUrl == "~/images/logo.png" && existing.SANPHAMs.Single().MaSanPham == "KEEP" && db.Rows.Find("HACK") == null, "activate; edit logo; key and related products cannot be overposted");
            var loaded = (BrandEditViewModel)((ViewResult)controller.Edit("OLD")).Model;
            Check(loaded.LogoUrl == existing.LogoUrl && loaded.MoTa == "Updated" && loaded.TrangThai, "GET edit loads saved fields");
            edit = Bind<BrandEditViewModel>(controller, "TenThuongHieu", "Existing", "TrangThai", "false");
            Check(controller.Edit("OLD", edit) is RedirectToRouteResult && !existing.TrangThai && existing.SANPHAMs.Count == 1, "deactivate without deleting related data");
            Check(existing.LogoUrl == null && controller.TempData["BrandSuccess"] != null, "clear logo and edit success message");
            var list = (IEnumerable<BrandViewModel>)((ViewResult)controller.Index()).Model;
            Check(list.Count() == 2 && list.Any(x => !x.TrangThai) && list.Any(x => x.TrangThai), "list includes both statuses");

            db.SaveError = new DbUpdateException("fixture save failure");
            edit = Bind<BrandEditViewModel>(controller, "TenThuongHieu", "Changed", "TrangThai", "false");
            Check(controller.Edit("OLD", edit) is ViewResult && controller.ModelState[""].Errors.Count > 0, "save failure returns form with safe message");
            db.SaveError = new DbUpdateConcurrencyException("fixture missing row");
            edit = Bind<BrandEditViewModel>(controller, "TenThuongHieu", "Changed", "TrangThai", "false");
            Check(controller.Edit("OLD", edit) is ViewResult && controller.ModelState[""].Errors.Count > 0, "row removed during save returns error");
        }

        var auth = (AuthorizeAttribute)Attribute.GetCustomAttribute(typeof(BrandsController), typeof(AuthorizeAttribute));
        Check(auth != null && auth.Roles == "ADMIN", "existing ADMIN authorization required");
        var posts = typeof(BrandsController).GetMethods().Where(x => Attribute.IsDefined(x, typeof(HttpPostAttribute))).ToArray();
        Check(posts.Length == 2 && posts.All(x => Attribute.IsDefined(x, typeof(ValidateAntiForgeryTokenAttribute))), "all mutation actions require POST and anti-forgery");
        Check(!typeof(BrandsController).GetMethods().Any(x => x.Name == "Delete"), "no hard delete endpoint");
        CompileViews(args[0]);
        Console.WriteLine("Completed " + checks + " database-free checks. SQL collation, constraints, HTTP authorization and concurrency are NOT verified.");
    }
}

class TestHttpContext : HttpContextBase
{
    readonly System.Collections.IDictionary items = new System.Collections.Hashtable();
    public override System.Collections.IDictionary Items { get { return items; } }
}

class FakeContext : QLHANGHOA_PHUKIEN_WEBEntities
{
    public readonly MemoryBrands Rows = new MemoryBrands();
    public int Saves;
    public Exception SaveError;
    public override DbSet<THUONGHIEU> THUONGHIEUx { get { return Rows; } set { } }
    public override int SaveChanges()
    {
        if (SaveError != null) throw SaveError;
        Saves++;
        return 1;
    }
}

class MemoryBrands : DbSet<THUONGHIEU>, IQueryable<THUONGHIEU>
{
    readonly List<THUONGHIEU> rows = new List<THUONGHIEU>();
    IQueryable<THUONGHIEU> Query { get { return rows.AsQueryable(); } }
    public override THUONGHIEU Add(THUONGHIEU entity) { rows.Add(entity); return entity; }
    public override THUONGHIEU Find(params object[] keys) { return rows.SingleOrDefault(x => x.MaThuongHieu == (string)keys[0]); }
    Type IQueryable.ElementType { get { return Query.ElementType; } }
    Expression IQueryable.Expression { get { return Query.Expression; } }
    IQueryProvider IQueryable.Provider { get { return Query.Provider; } }
    public IEnumerator<THUONGHIEU> GetEnumerator() { return rows.GetEnumerator(); }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return rows.GetEnumerator(); }
}

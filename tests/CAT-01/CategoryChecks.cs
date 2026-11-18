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

class CategoryChecks
{
    static int checks;
    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        checks++;
        Console.WriteLine("PASS: " + name);
    }

    static T Bind<T>(CategoriesController controller, params string[] pairs) where T : class
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

    static CategoriesController Controller(FakeContext context)
    {
        var controller = new CategoriesController();
        var field = typeof(CategoriesController).GetField("db", BindingFlags.Instance | BindingFlags.NonPublic);
        ((IDisposable)field.GetValue(controller)).Dispose();
        // Keep the production controller constructor unchanged; replace its disconnected context only in this fixture.
        field.SetValue(controller, context);
        controller.ControllerContext = new ControllerContext { Controller = controller, HttpContext = new TestHttpContext() };
        return controller;
    }

    static void InvalidCreate(string label, string code, string name, string description, string errorKey)
    {
        var db = new FakeContext();
        using (var controller = Controller(db))
        {
            var model = Bind<CategoryViewModel>(controller, "MaLoai", code, "TenLoai", name, "MoTa", description);
            Check(controller.Create(model) is ViewResult && controller.ModelState[errorKey].Errors.Count > 0
                && db.Saves == 0 && db.Rows.Count() == 0, label);
        }
    }

    static void CompileViews(string project)
    {
        foreach (var file in Directory.GetFiles(Path.Combine(project, "Areas/Admin/Views/Categories"), "*.cshtml"))
        {
            var host = new MvcWebPageRazorHost("~/Areas/Admin/Views/Categories/" + Path.GetFileName(file), file);
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
        InvalidCreate("missing name", "NEW", "", "", "TenLoai");
        InvalidCreate("whitespace name", "NEW", "   ", "", "TenLoai");
        InvalidCreate("missing code", "", "New", "", "MaLoai");
        InvalidCreate("invalid code characters", "ĐT/01", "New", "", "MaLoai");
        InvalidCreate("code longer than 10", new string('A', 11), "New", "", "MaLoai");
        InvalidCreate("name longer than 100", "NEW", new string('A', 101), "", "TenLoai");
        InvalidCreate("description longer than 500", "NEW", "New", new string('A', 501), "MoTa");

        var db = new FakeContext();
        var existing = new LOAISANPHAM { MaLoai = "OLD", TenLoai = "Existing", TrangThai = false };
        existing.SANPHAMs.Add(new SANPHAM { MaSanPham = "KEEP", MaLoai = "OLD" });
        db.Rows.Add(existing);
        using (var controller = Controller(db))
        {
            var model = Bind<CategoryViewModel>(controller, "MaLoai", "OLD", "TenLoai", "Different");
            Check(controller.Create(model) is ViewResult && controller.ModelState["MaLoai"].Errors.Count > 0 && db.Saves == 0, "duplicate code rejected");
            model = Bind<CategoryViewModel>(controller, "MaLoai", "NEW", "TenLoai", " Existing ");
            Check(controller.Create(model) is ViewResult && controller.ModelState["TenLoai"].Errors.Count > 0 && db.Saves == 0, "duplicate inactive name rejected after trim");

            model = Bind<CategoryViewModel>(controller, "MaLoai", "A_123456-0", "TenLoai", new string('N', 100), "MoTa", new string('D', 500), "TrangThai", "true");
            Check(controller.Create(model) is RedirectToRouteResult && db.Saves == 1 && db.Rows.Find(model.MaLoai).TrangThai, "create at all length boundaries");
            Check(controller.TempData["CategorySuccess"] != null, "create success message");
            Check(controller.Edit("missing") is HttpNotFoundResult && controller.Edit(null) is HttpNotFoundResult, "GET edit missing record");
            var edit = Bind<CategoryEditViewModel>(controller, "TenLoai", "Existing", "TrangThai", "true");
            Check(controller.Edit("missing", edit) is HttpNotFoundResult && db.Saves == 1, "POST edit missing record does not save");
            edit = Bind<CategoryEditViewModel>(controller, "TenLoai", new string('N', 100), "TrangThai", "true");
            Check(controller.Edit("OLD", edit) is ViewResult && controller.ModelState["TenLoai"].Errors.Count > 0 && db.Saves == 1, "edit duplicate name rejected");
            edit = Bind<CategoryEditViewModel>(controller, "TenLoai", "", "TrangThai", "true");
            Check(controller.Edit("OLD", edit) is ViewResult && controller.ModelState["TenLoai"].Errors.Count > 0 && db.Saves == 1, "edit missing name does not save");

            edit = Bind<CategoryEditViewModel>(controller, "TenLoai", "Existing", "MoTa", " Updated ", "TrangThai", "true", "MaLoai", "HACK", "SANPHAMs[0].MaSanPham", "HACK");
            Check(controller.Edit("OLD", edit) is RedirectToRouteResult && existing.TrangThai && existing.MoTa == "Updated" && existing.MaLoai == "OLD"
                && existing.SANPHAMs.Single().MaSanPham == "KEEP" && db.Rows.Find("HACK") == null, "activate; key and related products cannot be overposted");
            edit = Bind<CategoryEditViewModel>(controller, "TenLoai", "Existing", "TrangThai", "false");
            Check(controller.Edit("OLD", edit) is RedirectToRouteResult && !existing.TrangThai && existing.SANPHAMs.Count == 1, "deactivate without deleting related data");
            var list = (IEnumerable<CategoryViewModel>)((ViewResult)controller.Index()).Model;
            Check(list.Count() == 2 && list.Any(x => !x.TrangThai) && list.Any(x => x.TrangThai), "list includes both statuses");

            db.SaveError = new DbUpdateException("fixture save failure");
            edit = Bind<CategoryEditViewModel>(controller, "TenLoai", "Changed", "TrangThai", "false");
            Check(controller.Edit("OLD", edit) is ViewResult && controller.ModelState[""].Errors.Count > 0, "save failure returns form with safe message");
            db.SaveError = new DbUpdateConcurrencyException("fixture missing row");
            edit = Bind<CategoryEditViewModel>(controller, "TenLoai", "Changed", "TrangThai", "false");
            Check(controller.Edit("OLD", edit) is ViewResult && controller.ModelState[""].Errors.Count > 0, "row removed during save returns error");
        }

        var auth = (AuthorizeAttribute)Attribute.GetCustomAttribute(typeof(CategoriesController), typeof(AuthorizeAttribute));
        Check(auth != null && auth.Roles == "ADMIN", "existing ADMIN authorization required");
        var posts = typeof(CategoriesController).GetMethods().Where(x => Attribute.IsDefined(x, typeof(HttpPostAttribute))).ToArray();
        Check(posts.Length == 2 && posts.All(x => Attribute.IsDefined(x, typeof(ValidateAntiForgeryTokenAttribute))), "all mutation actions require POST and anti-forgery");
        Check(!typeof(CategoriesController).GetMethods().Any(x => x.Name == "Delete"), "no hard delete endpoint");
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
    public readonly MemoryCategories Rows = new MemoryCategories();
    public int Saves;
    public Exception SaveError;
    public override DbSet<LOAISANPHAM> LOAISANPHAMs { get { return Rows; } set { } }
    public override int SaveChanges()
    {
        if (SaveError != null) throw SaveError;
        Saves++;
        return 1;
    }
}

class MemoryCategories : DbSet<LOAISANPHAM>, IQueryable<LOAISANPHAM>
{
    readonly List<LOAISANPHAM> rows = new List<LOAISANPHAM>();
    IQueryable<LOAISANPHAM> Query { get { return rows.AsQueryable(); } }
    public override LOAISANPHAM Add(LOAISANPHAM entity) { rows.Add(entity); return entity; }
    public override LOAISANPHAM Find(params object[] keys) { return rows.SingleOrDefault(x => x.MaLoai == (string)keys[0]); }
    Type IQueryable.ElementType { get { return Query.ElementType; } }
    Expression IQueryable.Expression { get { return Query.Expression; } }
    IQueryProvider IQueryable.Provider { get { return Query.Provider; } }
    public IEnumerator<LOAISANPHAM> GetEnumerator() { return rows.GetEnumerator(); }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return rows.GetEnumerator(); }
}

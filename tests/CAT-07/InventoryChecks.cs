using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Data.Entity;
using System.CodeDom.Compiler;
using System.Web.Mvc;
using System.Web.Mvc.Razor;
using System.Web.Razor;
using Microsoft.CSharp;
using QuanLiDienThoaiDD.Areas.Admin.Controllers;
using QuanLiDienThoaiDD.Areas.Admin.ViewModels;
using QuanLiDienThoaiDD.Models;

class InventoryChecks
{
    static int checks;
    static void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL: " + name); checks++; Console.WriteLine("PASS: " + name); }
    static InventoryController Controller(QLHANGHOA_PHUKIEN_WEBEntities db)
    {
        var c = new InventoryController();
        var field = typeof(InventoryController).GetField("db", BindingFlags.Instance | BindingFlags.NonPublic);
        ((IDisposable)field.GetValue(c)).Dispose(); field.SetValue(c, db);
        c.ControllerContext = new ControllerContext { Controller = c, HttpContext = new TestHttpContext() };
        return c;
    }
    static InventoryRowViewModel[] Rows(ActionResult result) { return ((System.Collections.Generic.IEnumerable<InventoryRowViewModel>)((ViewResult)result).Model).ToArray(); }
    static void CompileView(string project)
    {
        var file = Path.Combine(project, "Areas/Admin/Views/Inventory/Index.cshtml");
        var host = new MvcWebPageRazorHost("~/Areas/Admin/Views/Inventory/Index.cshtml", file);
        foreach (var ns in new[] { "System", "System.Linq", "System.Collections.Generic", "System.Web.Mvc", "System.Web.Mvc.Html" }) host.NamespaceImports.Add(ns);
        using (var reader = File.OpenText(file)) using (var compiler = new CSharpCodeProvider())
        {
            var generated = new RazorTemplateEngine(host).GenerateCode(reader);
            Check(generated.Success, "Razor parse inventory");
            var options = new CompilerParameters { GenerateInMemory = true };
            foreach (var assembly in new[] { "System.dll", "System.Core.dll", "Microsoft.CSharp.dll", "System.Web.dll", "System.ComponentModel.DataAnnotations.dll" }) options.ReferencedAssemblies.Add(assembly);
            foreach (var assembly in new[] { "QuanLiDienThoaiDD.dll", "System.Web.Mvc.dll", "System.Web.WebPages.dll", "System.Web.WebPages.Razor.dll", "System.Web.Razor.dll", "System.Web.Helpers.dll" }) options.ReferencedAssemblies.Add(Path.Combine(project, "bin", assembly));
            var result = compiler.CompileAssemblyFromDom(options, generated.GeneratedCode);
            if (result.Errors.HasErrors) throw new Exception(string.Join(Environment.NewLine, result.Errors.Cast<CompilerError>().Select(x => x.ToString())));
            Check(!result.Errors.HasErrors, "Razor compile inventory");
        }
        var links = File.ReadAllLines(file).Where(x => x.Contains("page =")).ToArray();
        Check(links.Length == 4 && links.All(x => new[] { "q =", "state =", "productState =", "stock =" }.All(x.Contains)), "pager route declarations preserve filters");
    }
    public static void Main(string[] args)
    {
        var db = new FakeContext();
        db.Products.Add(new SANPHAM { MaSanPham = "P", TenSanPham = "Phone", TrangThai = false });
        db.Products.Add(new SANPHAM { MaSanPham = "Q", TenSanPham = "Tablet", TrangThai = true });
        for (int i = 0; i < 45; i++) db.Variants.Add(new BIENSANPHAM { MaBienSanPham = "V" + i.ToString("D2"), MaSanPham = i < 30 ? "P" : "Q", SKU = "SKU" + i.ToString("D2"), TrangThai = i % 2 == 0 });
        db.Stocks.Add(new TONKHO { MaBienSanPham = "V00", SoLuongTon = 7, SoLuongDangDat = 9, NgayCapNhat = new DateTime(2020, 1, 1) });
        db.Stocks.Add(new TONKHO { MaBienSanPham = "V01", SoLuongTon = 0, SoLuongDangDat = 0, NgayCapNhat = new DateTime(2021, 1, 1) });
        using (var c = Controller(db))
        {
            var first = Rows(c.Index());
            Check(first.Length == 20 && (int)c.ViewBag.Total == 45 && (int)c.ViewBag.Pages == 3, "variants without TONKHO included and bounded page");
            Check(first[0].SoLuongTon == 7 && first[0].SoLuongDangDat == 9 && first[0].NgayCapNhat == new DateTime(2020, 1, 1), "raw stock, reserved greater than stock and timestamp preserved");
            Check(first[1].SoLuongTon == 0 && first[1].NgayCapNhat.HasValue, "existing zero stock remains zero");
            Check(!first[2].SoLuongTon.HasValue && !first[2].SoLuongDangDat.HasValue && !first[2].NgayCapNhat.HasValue, "missing stock quantities and date remain unknown");
            Check(!first[0].ProductActive && first[0].VariantActive, "independent product and variant statuses");
            var second = Rows(c.Index(page: "2"));
            Check(second.Length == 20 && !second.Any(x => first.Any(y => x.MaBienSanPham == y.MaBienSanPham)), "stable nonoverlapping pages");
            Check(Rows(c.Index(page: "3")).Length == 5, "last page remainder");
            foreach (var page in new[] { "0", "-1", "abc", "1.5", "99999999999999" })
                Check(Rows(c.Index(page: page))[0].MaBienSanPham == "V00" && (bool)c.ViewBag.Adjusted, "invalid page " + page);
            Check(Rows(c.Index(page: int.MaxValue.ToString())).Length == 5 && (int)c.ViewBag.Page == 3, "huge page clamps safely");
            foreach (var q in new[] { "V00", "SKU00" }) Check(Rows(c.Index(q: " " + q + " ")).Single().MaBienSanPham == "V00", "search and trim " + q);
            Check(Rows(c.Index(q: "Phone")).Length == 20 && (int)c.ViewBag.Total == 30, "product name search");
            Check(Rows(c.Index(q: "Q")).Length == 15, "product code search");
            Check(Rows(c.Index(stock: "missing")).Length == 20 && (int)c.ViewBag.Total == 43, "missing stock filter before paging");
            Check(Rows(c.Index(stock: "present")).Length == 2, "present filter includes zero stock");
            Check(Rows(c.Index(state: "inactive", productState: "inactive", stock: "present")).Single().MaBienSanPham == "V01", "combined filters include inactive data");
            Check(Rows(c.Index(state: "active", productState: "active")).All(x => x.VariantActive && x.ProductActive), "both active filters");
            Check(Rows(c.Index(q: "none", page: "4")).Length == 0 && (int)c.ViewBag.Page == 1 && (int)c.ViewBag.Total == 0, "empty result and page clamp");
            Check(Rows(c.Index(state: "bad", productState: "bad", stock: "bad")).Length == 20 && (bool)c.ViewBag.Adjusted, "invalid filters normalize");
            Check(((HttpStatusCodeResult)c.Index(q: new string('q', 201))).StatusCode == 400, "search length rejected");
            Check(db.Saves == 0 && db.Stocks.Count() == 2 && db.Stocks.Find("V00").SoLuongTon == 7 && db.Stocks.Find("V00").SoLuongDangDat == 9, "GET neither creates nor updates stock");
        }
        var auth = (AuthorizeAttribute)Attribute.GetCustomAttribute(typeof(InventoryController), typeof(AuthorizeAttribute));
        Check(auth != null && auth.Roles == "ADMIN", "ADMIN metadata");
        var actions = typeof(InventoryController).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Check(actions.Length == 1 && actions[0].Name == "Index" && Attribute.IsDefined(actions[0], typeof(HttpGetAttribute)), "only GET Index action; no stock mutation endpoint");
        Database.SetInitializer<QLHANGHOA_PHUKIEN_WEBEntities>(null);
        using (var real = new QLHANGHOA_PHUKIEN_WEBEntities()) using (var c = Controller(real))
        foreach (var stock in new[] { "", "missing", "present" })
        {
            var query = (IQueryable<InventoryRowViewModel>)typeof(InventoryController).GetMethod("InventoryQuery", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(c, new object[] { "Phone", "active", "inactive", stock });
            var sql = query.OrderBy(x => x.MaBienSanPham).Skip(20).Take(20).ToString();
            Check(sql.Contains("LEFT OUTER JOIN") && sql.Contains("TONKHO") && sql.Contains("ORDER BY"), "EF6 offline SQL generation with left join and stock filter " + stock);
        }
        CompileView(args[0]);
        Console.WriteLine("Completed " + checks + " checks: fake data, offline EF6 SQL, Razor and metadata. SQL execution/HTTP/UI/concurrency UNVERIFIED.");
    }
}

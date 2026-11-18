using System;
using System.Linq;
using System.Reflection;
using System.Data.Entity;
using System.Web.Mvc;
using QuanLiDienThoaiDD.Areas.Admin.Controllers;
using QuanLiDienThoaiDD.Areas.Admin.ViewModels;
using QuanLiDienThoaiDD.Models;

class BrowseChecks
{
    static int checks;
    static void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL: " + name); checks++; Console.WriteLine("PASS: " + name); }
    static ProductsController Controller(QLHANGHOA_PHUKIEN_WEBEntities db)
    {
        var c = new ProductsController();
        var field = typeof(ProductsController).GetField("db", BindingFlags.Instance | BindingFlags.NonPublic);
        ((IDisposable)field.GetValue(c)).Dispose(); field.SetValue(c, db);
        c.ControllerContext = new ControllerContext { Controller = c, HttpContext = new TestHttpContext() };
        return c;
    }
    static ProductViewModel[] Rows(ActionResult result) { return ((System.Collections.Generic.IEnumerable<ProductViewModel>)((ViewResult)result).Model).ToArray(); }
    public static void Main(string[] args)
    {
        var db = new FakeContext();
        db.Categories.Add(new LOAISANPHAM { MaLoai = "C", TenLoai = "Category", TrangThai = false });
        db.Brands.Add(new THUONGHIEU { MaThuongHieu = "B", TenThuongHieu = "Brand", TrangThai = false });
        for (int i = 0; i < 45; i++) db.Products.Add(new SANPHAM { MaSanPham = "P" + i.ToString("D2"), TenSanPham = "Phone", MaLoai = i % 2 == 0 ? "C" : "D", MaThuongHieu = "B", TrangThai = i % 2 == 0, NgayTao = new DateTime(2020, 1, 1).AddDays(i) });
        db.Variants.Add(new BIENSANPHAM { MaBienSanPham = "V1", MaSanPham = "P03", SKU = "needle-1", TrangThai = false });
        db.Variants.Add(new BIENSANPHAM { MaBienSanPham = "V2", MaSanPham = "P03", SKU = "needle-2" });
        using (var c = Controller(db))
        {
            var first = Rows(c.Index());
            Check(first.Length == 20 && first[0].MaSanPham == "P00" && (int)c.ViewBag.Total == 45 && (int)c.ViewBag.Pages == 3, "first page bounded, count and stable name ties");
            var second = Rows(c.Index(page: "2"));
            Check(second.Length == 20 && second[0].MaSanPham == "P20" && !second.Any(x => first.Any(y => x.MaSanPham == y.MaSanPham)), "second page no overlap");
            Check(Rows(c.Index(page: "3")).Length == 5, "last page remainder");
            Check(Rows(c.Index(page: int.MaxValue.ToString())).Length == 5 && (int)c.ViewBag.Page == 3, "huge page clamps without overflow");
            foreach (var page in new[] { "0", "-1", "abc", "1.5", "999999999999999999999" })
                Check(Rows(c.Index(page: page))[0].MaSanPham == "P00" && (bool)c.ViewBag.Adjusted, "invalid page " + page);
            Check(Rows(c.Index(q: " P03 ")).Single().MaSanPham == "P03" && (string)c.ViewBag.Search == "P03", "trim and code search");
            Check(Rows(c.Index(q: "Phone")).Length == 20 && (int)c.ViewBag.Total == 45, "name search count before paging");
            Check(Rows(c.Index(q: "needle")).Single().MaSanPham == "P03", "SKU EXISTS includes inactive variants and avoids duplicate products");
            Check(Rows(c.Index(category: "C", brand: "B", state: "active", page: "2")).Length == 3 && (int)c.ViewBag.Total == 23, "combined filters before paging, inactive lookup retained");
            Check(Rows(c.Index(state: "inactive")).All(x => !x.TrangThai) && (int)c.ViewBag.Total == 22, "inactive product filter");
            foreach (var sort in new[] { "name", "name_desc", "oldest", "code" })
                Check(Rows(c.Index(sort: sort))[0].MaSanPham == "P00", "sort " + sort + " deterministic");
            Check(Rows(c.Index(sort: "newest"))[0].MaSanPham == "P44", "newest creation date");
            db.Products.Find("P00").TenSanPham = "Apple"; db.Products.Find("P44").TenSanPham = "Zulu";
            Check(Rows(c.Index(sort: "name"))[0].MaSanPham == "P00" && Rows(c.Index(sort: "name_desc"))[0].MaSanPham == "P44", "name ascending and descending with distinct values");
            Check(Rows(c.Index(sort: "bad", state: "bad")).Length == 20 && (string)c.ViewBag.Sort == "name" && (bool)c.ViewBag.Adjusted, "invalid sort/state normalized");
            Check(Rows(c.Index(category: "missing")).Length == 0 && (int)c.ViewBag.Page == 1 && (int)c.ViewBag.Total == 0, "unknown key gives empty result");
            Check(Rows(c.Index(q: "no match", page: "5")).Length == 0 && (int)c.ViewBag.Pages == 1, "empty result page bounded");
            Check(c.Index(q: new string('q', 201)) is HttpStatusCodeResult && c.Index(category: new string('c', 11)) is HttpStatusCodeResult && c.Index(brand: new string('b', 11)) is HttpStatusCodeResult, "oversized filters rejected");
            Check(db.Saves == 0 && db.StockAccesses == 0, "browse never writes or reads inventory");
        }
        var links = System.IO.File.ReadAllLines(System.IO.Path.Combine(args[0], "Areas/Admin/Views/Products/Index.cshtml")).Where(x => x.Contains("page =")).ToArray();
        Check(links.Length == 4 && links.All(x => new[] { "q =", "category =", "brand =", "state =", "sort =" }.All(x.Contains)), "all pager route declarations retain filter and sort parameters (source check)");
        Database.SetInitializer<QLHANGHOA_PHUKIEN_WEBEntities>(null);
        using (var real = new QLHANGHOA_PHUKIEN_WEBEntities()) using (var c = Controller(real))
        {
            foreach (var sort in new[] { "name", "name_desc", "newest", "oldest", "code" })
            {
                var query = (IQueryable<SANPHAM>)typeof(ProductsController).GetMethod("ProductQuery", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(c, new object[] { "needle%_", "C", "B", "active", sort });
                var sql = query.Skip(20).Take(20).Select(x => x.MaSanPham).ToString();
                Check(sql.Contains("EXISTS") && sql.Contains("ORDER BY") && sql.Contains("SANPHAM") && sql.Contains("BIENSANPHAM"), "EF6 SQL generation with filters, SKU and pagination: " + sort);
            }
        }
        Console.WriteLine("Completed " + checks + " checks: fake data and offline EF6 SQL generation. No SQL execution/IIS/UI acceptance.");
    }
}

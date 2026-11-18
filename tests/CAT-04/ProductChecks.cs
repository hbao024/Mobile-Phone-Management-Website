// No Application_Start, database, stock writes or seed. Real SQL/HTTP behavior remains unverified.
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

class ProductChecks
{
    static int checks;
    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        checks++;
        Console.WriteLine("PASS: " + name);
    }

    static T Controller<T>(FakeContext context) where T : Controller, new()
    {
        var controller = new T();
        var field = typeof(T).GetField("db", BindingFlags.Instance | BindingFlags.NonPublic);
        ((IDisposable)field.GetValue(controller)).Dispose();
        field.SetValue(controller, context);
        controller.ControllerContext = new ControllerContext { Controller = controller, HttpContext = new TestHttpContext() };
        return controller;
    }

    static T Bind<T>(Controller controller, params string[] pairs) where T : class
    { return BindCulture<T>(controller, CultureInfo.InvariantCulture, pairs); }

    static T BindCulture<T>(Controller controller, CultureInfo culture, params string[] pairs) where T : class
    {
        controller.ModelState.Clear();
        var values = new NameValueCollection();
        for (int i = 0; i < pairs.Length; i += 2) values.Add(pairs[i], pairs[i + 1]);
        return (T)new DefaultModelBinder().BindModel(controller.ControllerContext, new ModelBindingContext
        {
            ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(null, typeof(T)),
            ModelState = controller.ModelState,
            ValueProvider = new NameValueCollectionValueProvider(values, culture)
        });
    }

    static string[] Data(bool variant, params string[] overrides)
    {
        var defaults = variant
            ? new[] { "MaBienSanPham", "VNEW", "SKU", "NEW", "GiaNhap", "0", "GiaBan", "100", "TrangThai", "true" }
            : new[] { "MaSanPham", "PNEW", "TenSanPham", "Product", "MaLoai", "C", "MaThuongHieu", "B", "MaNCCMacDinh", "S", "BaoHanhThang", "12", "TrangThai", "true" };
        var values = new Dictionary<string, string>();
        for (int i = 0; i < defaults.Length; i += 2) values[defaults[i]] = defaults[i + 1];
        for (int i = 0; i < overrides.Length; i += 2) values[overrides[i]] = overrides[i + 1];
        return values.SelectMany(x => new[] { x.Key, x.Value }).ToArray();
    }

    static FakeContext Seed()
    {
        var db = new FakeContext();
        foreach (var suffix in new[] { "", "OLD", "OTHER" })
        {
            db.Categories.Add(new LOAISANPHAM { MaLoai = "C" + suffix, TenLoai = "Category" + suffix, TrangThai = suffix == "" });
            db.Brands.Add(new THUONGHIEU { MaThuongHieu = "B" + suffix, TenThuongHieu = "Brand" + suffix, TrangThai = suffix == "" });
            db.Suppliers.Add(new NHACUNGCAP { MaNCC = "S" + suffix, TenNCC = "Supplier" + suffix, TrangThai = suffix == "" });
        }
        foreach (var id in new[] { "P", "Q", "H" })
            db.Products.Add(new SANPHAM { MaSanPham = id, TenSanPham = "Product", MaLoai = id == "H" ? "COLD" : "C",
                MaThuongHieu = id == "H" ? "BOLD" : "B", MaNCCMacDinh = id == "H" ? "SOLD" : "S",
                BaoHanhThang = 12, TrangThai = id != "Q", NgayTao = new DateTime(2000, 1, 1), NgayCapNhat = new DateTime(2000, 1, 1) });
        var v = new BIENSANPHAM { MaBienSanPham = "V", MaSanPham = "P", SKU = "OLD", TrangThai = true, GiaNhap = 10, GiaBan = 20,
            HinhAnhUrl = "/images/old.jpg", TONKHO = new TONKHO { MaBienSanPham = "V", SoLuongTon = 7, SoLuongDangDat = 2 } };
        v.CHITIETDONHANGs.Add(new CHITIETDONHANG { MaDonHang = "ORDER", MaBienSanPham = "V", TenSanPham = "Snapshot", DonGia = 125m });
        v.CHITIETGIOHANGs.Add(new CHITIETGIOHANG { MaGioHang = 1, MaBienSanPham = "V", SoLuong = 3 });
        v.CHITIETPHIEUNHAPs.Add(new CHITIETPHIEUNHAP { MaPhieuNhap = "RECEIPT", MaBienSanPham = "V", DonGia = 60m });
        v.GIAODICHKHOes.Add(new GIAODICHKHO());
        v.DANHGIAs.Add(new DANHGIA());
        db.Products.Find("P").BIENSANPHAMs.Add(v);
        db.Variants.Add(v);
        db.Variants.Add(new BIENSANPHAM { MaBienSanPham = "OTHER", MaSanPham = "Q", SKU = "RESERVED", TrangThai = false });
        db.Stocks.Add(v.TONKHO);
        return db;
    }

    static void ProductValidation()
    {
        var invalid = new[] {
            new[] { "MaSanPham", "" }, new[] { "MaSanPham", "ĐT/01" }, new[] { "MaSanPham", new string('A', 21) }, new[] { "MaSanPham", "P" },
            new[] { "TenSanPham", "" }, new[] { "TenSanPham", "   " }, new[] { "TenSanPham", new string('N', 201) },
            new[] { "MaLoai", "" }, new[] { "MaLoai", new string('C', 11) }, new[] { "MaLoai", "missing" }, new[] { "MaLoai", "COLD" },
            new[] { "MaThuongHieu", "" }, new[] { "MaThuongHieu", new string('B', 11) }, new[] { "MaThuongHieu", "missing" }, new[] { "MaThuongHieu", "BOLD" },
            new[] { "MaNCCMacDinh", new string('S', 11) }, new[] { "MaNCCMacDinh", "missing" }, new[] { "MaNCCMacDinh", "SOLD" },
            new[] { "BaoHanhThang", "" }, new[] { "BaoHanhThang", "-1" }, new[] { "BaoHanhThang", "121" }, new[] { "BaoHanhThang", "32768" }, new[] { "BaoHanhThang", "1.5" }
        };
        foreach (var pair in invalid)
        {
            var db = Seed();
            using (var c = Controller<ProductsController>(db))
            {
                var create = Bind<ProductViewModel>(c, Data(false, pair));
                Check(c.Create(create) is ViewResult && c.ModelState[pair[0]].Errors.Count > 0 && db.Saves == 0 && db.Products.Count() == 3,
                    "product create rejects " + pair[0] + " length " + pair[1].Length);
                Check(((IEnumerable<SelectListItem>)c.ViewBag.CategoryOptions).All(x => x.Value == "C"), "failed create reloads active choices");
                if (pair[0] == "MaSanPham") continue;
                var edit = Bind<ProductEditViewModel>(c, Data(false, pair));
                Check(c.Edit("P", edit) is ViewResult && c.ModelState[pair[0]].Errors.Count > 0 && db.Saves == 0
                    && db.Products.Find("P").TenSanPham == "Product", "product edit rejects " + pair[0] + " length " + pair[1].Length);
            }
        }
    }

    static void Products()
    {
        var db = Seed();
        using (var c = Controller<ProductsController>(db))
        {
            var defaults = (ProductViewModel)((ViewResult)c.Create()).Model;
            Check(defaults.TrangThai && defaults.BaoHanhThang == 12, "product defaults");
            var model = Bind<ProductViewModel>(c, Data(false, "MaSanPham", new string('P', 20), "TenSanPham", new string('N', 200), "BaoHanhThang", "120",
                "MaNCCMacDinh", "", "MoTa", new string('D', 2000), "NgayTao", "1900-01-01", "BIENSANPHAMs[0].MaBienSanPham", "HACK"));
            Check(c.Create(model) is RedirectToRouteResult && db.Saves == 1 && c.TempData["ProductSuccess"] != null, "create product at SQL boundaries");
            var added = db.Products.Find(model.MaSanPham);
            Check(added.MoTa.Length == 2000 && added.MaNCCMacDinh == null && added.NgayTao.Year != 1900 && added.BIENSANPHAMs.Count == 0,
                "nvarchar max description, optional supplier, dates and variants cannot be overposted");
            model = Bind<ProductViewModel>(c, Data(false, "TenSanPham", "Product", "BaoHanhThang", "0"));
            Check(c.Create(model) is RedirectToRouteResult && db.Products.Find("PNEW").BaoHanhThang == 0, "duplicate product name allowed, zero warranty");
            Check(c.Edit(null) is HttpNotFoundResult && c.Edit("missing") is HttpNotFoundResult, "product GET missing");
            var edit = Bind<ProductEditViewModel>(c, Data(false));
            Check(c.Edit(null, edit) is HttpNotFoundResult && c.Edit("missing", edit) is HttpNotFoundResult && db.Saves == 2, "product POST missing");

            var history = (ProductEditViewModel)((ViewResult)c.Edit("H")).Model;
            Check(history.MaLoai == "COLD" && history.MaThuongHieu == "BOLD" && history.MaNCCMacDinh == "SOLD", "GET retains historical inactive links");
            Check(((IEnumerable<SelectListItem>)c.ViewBag.CategoryOptions).Select(x => x.Value).OrderBy(x => x).SequenceEqual(new[] { "C", "COLD" })
                && ((IEnumerable<SelectListItem>)c.ViewBag.BrandOptions).Count() == 2 && ((IEnumerable<SelectListItem>)c.ViewBag.SupplierOptions).Count() == 2,
                "choices include only active and original inactive records");
            edit = Bind<ProductEditViewModel>(c, Data(false, "MaLoai", "COLD", "MaThuongHieu", "BOLD", "MaNCCMacDinh", "SOLD"));
            Check(c.Edit("H", edit) is RedirectToRouteResult, "save unchanged historical inactive links");
            foreach (var pair in new[] { new[] { "MaLoai", "COTHER" }, new[] { "MaThuongHieu", "BOTHER" }, new[] { "MaNCCMacDinh", "SOTHER" } })
            {
                edit = Bind<ProductEditViewModel>(c, Data(false, pair));
                Check(c.Edit("H", edit) is ViewResult && c.ModelState[pair[0]].Errors.Count > 0, "cannot switch to another inactive " + pair[0]);
            }
            edit = Bind<ProductEditViewModel>(c, Data(false, "MaNCCMacDinh", ""));
            Check(c.Edit("H", edit) is RedirectToRouteResult && db.Products.Find("H").MaLoai == "C" && db.Products.Find("H").MaNCCMacDinh == null,
                "switch historical links to active and clear optional supplier");
            edit = Bind<ProductEditViewModel>(c, Data(false, "TrangThai", "false", "MaSanPham", "HACK", "NgayTao", "1900-01-01", "NgayCapNhat", "1900-01-01",
                "BIENSANPHAMs[0].SKU", "HACK"));
            Check(c.Edit("P", edit) is RedirectToRouteResult && !db.Products.Find("P").TrangThai && db.Products.Find("P").MaSanPham == "P"
                && db.Products.Find("P").NgayTao.Year == 2000 && db.Products.Find("P").NgayCapNhat.Year != 1900
                && db.Variants.Find("V").SKU == "OLD" && db.Variants.Find("V").TrangThai, "deactivate parent without cascading, preserve identity date and variants");
            edit = Bind<ProductEditViewModel>(c, Data(false));
            Check(c.Edit("P", edit) is RedirectToRouteResult && db.Products.Find("P").TrangThai, "reactivate product");
            var list = (IEnumerable<ProductViewModel>)((ViewResult)c.Index()).Model;
            Check(list.Count() == 5 && list.Any(x => !x.TrangThai) && list.Any(x => x.TrangThai), "products list both statuses");
            db.SaveError = new DbUpdateException("SQL detail must not leak");
            edit = Bind<ProductEditViewModel>(c, Data(false));
            Check(c.Edit("P", edit) is ViewResult && c.ModelState[""].Errors.Count > 0 && !c.ModelState[""].Errors[0].ErrorMessage.Contains("SQL detail"), "product save safe error");
            model = Bind<ProductViewModel>(c, Data(false, "MaSanPham", "FAIL"));
            Check(c.Create(model) is ViewResult && c.ModelState[""].Errors.Count > 0, "product create save failure");
            db.SaveError = new DbUpdateConcurrencyException("fixture deleted row");
            edit = Bind<ProductEditViewModel>(c, Data(false));
            Check(c.Edit("P", edit) is ViewResult && c.ModelState[""].Errors.Count > 0, "product concurrent disappearance");
            Check(db.StockAccesses == 0 && db.Stocks.Count() == 1, "product never touches stock");
        }
    }

    static void VariantValidation()
    {
        var invalid = new List<string[]> {
            new[] { "MaBienSanPham", "" }, new[] { "MaBienSanPham", "ĐT/01" }, new[] { "MaBienSanPham", new string('V', 31) }, new[] { "MaBienSanPham", "V" },
            new[] { "SKU", "" }, new[] { "SKU", "   " }, new[] { "SKU", "A B" }, new[] { "SKU", "ĐT" }, new[] { "SKU", new string('S', 51) },
            new[] { "SKU", "RESERVED" }, new[] { "MauSac", new string('C', 51) },
            new[] { "RAMGB", "0" }, new[] { "RAMGB", "-1" }, new[] { "RAMGB", "32768" }, new[] { "RAMGB", "1.5" },
            new[] { "BoNhoGB", "0" }, new[] { "BoNhoGB", "-1" }, new[] { "BoNhoGB", "32768" },
            new[] { "HinhAnhUrl", "/images/" + new string('a', 493) }, new[] { "HinhAnhUrl", "/images/../bad.jpg" },
            new[] { "HinhAnhUrl", "/images/%2e%2e/bad.jpg" }, new[] { "HinhAnhUrl", "/other/logo.jpg" }, new[] { "HinhAnhUrl", "javascript:alert(1)" },
            new[] { "HinhAnhUrl", "//evil.test/a.jpg" }, new[] { "HinhAnhUrl", "/images/a'bad.jpg" }, new[] { "HinhAnhUrl", "/images/a\\bad.jpg" },
            new[] { "HinhAnhUrl", "/images/a\nbad.jpg" }, new[] { "HinhAnhUrl", "https://example.com/a.jpg" }, new[] { "HinhAnhUrl", "~/images/a.jpg" }
        };
        foreach (var price in new[] { "GiaNhap", "GiaBan" })
            foreach (var amount in new[] { "", "-0.01", "10000000000000000", "1.001", "NaN", "abc", "79228162514264337593543950336" })
                invalid.Add(new[] { price, amount });
        foreach (var pair in invalid)
        {
            var db = Seed();
            using (var c = Controller<ProductVariantsController>(db))
            {
                var create = Bind<ProductVariantViewModel>(c, Data(true, pair));
                Check(c.Create("P", create) is ViewResult && c.ModelState[pair[0]].Errors.Count > 0 && db.Saves == 0 && db.Variants.Count() == 2,
                    "variant create rejects " + pair[0] + " length " + pair[1].Length);
                if (pair[0] == "MaBienSanPham") continue;
                var edit = Bind<ProductVariantEditViewModel>(c, Data(true, pair));
                Check(c.Edit("P", "V", edit) is ViewResult && c.ModelState[pair[0]].Errors.Count > 0 && db.Saves == 0
                    && db.Variants.Find("V").GiaBan == 20 && db.Variants.Find("V").SKU == "OLD", "variant edit rejects " + pair[0] + " length " + pair[1].Length);
            }
        }
    }

    static void Variants()
    {
        var db = Seed();
        using (var c = Controller<ProductVariantsController>(db))
        {
            Check(c.Index(null) is HttpNotFoundResult && c.Index("missing") is HttpNotFoundResult && c.Create("missing") is HttpNotFoundResult, "variant parent GET missing");
            var model = Bind<ProductVariantViewModel>(c, Data(true));
            Check(c.Create("missing", model) is HttpNotFoundResult && db.Saves == 0, "variant parent POST missing");
            Check(((ProductVariantViewModel)((ViewResult)c.Create("P")).Model).TrangThai, "variant default active");
            model = Bind<ProductVariantViewModel>(c, Data(true, "MaBienSanPham", new string('V', 30), "SKU", new string('S', 50), "MauSac", new string('C', 50),
                "RAMGB", "32767", "BoNhoGB", "32767", "GiaNhap", "9999999999999999.99", "GiaBan", "9999999999999999.99", "HinhAnhUrl", "/images/" + new string('a', 492),
                "MaSanPham", "Q", "TONKHO.SoLuongTon", "999", "CHITIETDONHANGs[0].DonGia", "999"));
            Check(c.Create("P", model) is RedirectToRouteResult && db.Saves == 1, "variant create at SQL boundaries");
            var added = db.Variants.Find(model.MaBienSanPham);
            Check(added.MaSanPham == "P" && added.TONKHO == null && added.CHITIETDONHANGs.Count == 0 && added.GiaBan == 9999999999999999.99m
                && added.HinhAnhUrl.Length == 500, "parent from route; no stock/order overposting");
            Check(c.TempData["VariantSuccess"] != null, "variant create success message");
            model = Bind<ProductVariantViewModel>(c, Data(true, "SKU", "CLONE", "GiaNhap", "0", "GiaBan", "0", "MauSac", " ", "RAMGB", "", "BoNhoGB", "", "HinhAnhUrl", ""));
            Check(c.Create("Q", model) is RedirectToRouteResult && db.Variants.Find("VNEW").MaSanPham == "Q" && db.Variants.Find("VNEW").RAMGB == null
                && db.Variants.Find("VNEW").HinhAnhUrl == null, "zero prices, optional attributes and inactive parent manageable");
            model = Bind<ProductVariantViewModel>(c, Data(true, "MaBienSanPham", "CLONE2", "SKU", "CLONE2"));
            Check(c.Create("Q", model) is RedirectToRouteResult, "same optional attribute combination allowed with distinct SKU");
            var edit = Bind<ProductVariantEditViewModel>(c, Data(true));
            var before = db.Saves;
            Check(c.Edit("Q", "V") is HttpNotFoundResult && c.Edit("P", "missing") is HttpNotFoundResult && c.Edit("P", null) is HttpNotFoundResult, "GET variant ownership and missing");
            Check(c.Edit("Q", "V", edit) is HttpNotFoundResult && c.Edit("P", "missing", edit) is HttpNotFoundResult
                && c.Edit("missing", "V", edit) is HttpNotFoundResult && db.Saves == before, "POST wrong parent or missing does not save");
            var loaded = (ProductVariantEditViewModel)((ViewResult)c.Edit("P", "V")).Model;
            Check(loaded.SKU == "OLD" && loaded.GiaNhap == 10 && loaded.GiaBan == 20 && loaded.HinhAnhUrl == "/images/old.jpg", "variant GET fields");
            edit = Bind<ProductVariantEditViewModel>(c, Data(true, "SKU", "OLD", "GiaNhap", "11.23", "GiaBan", "22.34", "TrangThai", "false",
                "MaBienSanPham", "HACK", "MaSanPham", "Q", "TONKHO.SoLuongTon", "999", "TONKHO.SoLuongDangDat", "999",
                "CHITIETDONHANGs[0].DonGia", "999", "CHITIETGIOHANGs[0].SoLuong", "999", "CHITIETPHIEUNHAPs[0].DonGia", "999"));
            Check(c.Edit("P", "V", edit) is RedirectToRouteResult, "own SKU retained and deactivate variant");
            var v = db.Variants.Find("V");
            Check(v.MaBienSanPham == "V" && v.MaSanPham == "P" && !v.TrangThai && v.GiaNhap == 11.23m && v.GiaBan == 22.34m
                && v.HinhAnhUrl == null && v.RAMGB == null, "variant scalar updates, immutable key and parent, clear image");
            Check(v.TONKHO.SoLuongTon == 7 && v.TONKHO.SoLuongDangDat == 2 && v.CHITIETDONHANGs.Single().DonGia == 125
                && v.CHITIETDONHANGs.Single().TenSanPham == "Snapshot" && v.CHITIETGIOHANGs.Single().SoLuong == 3
                && v.CHITIETPHIEUNHAPs.Single().DonGia == 60 && v.GIAODICHKHOes.Count == 1 && v.DANHGIAs.Count == 1,
                "stock, order snapshot, cart, receipts and other relations unchanged");
            edit = Bind<ProductVariantEditViewModel>(c, Data(true, "SKU", "UPDATED", "MauSac", " Blue ", "RAMGB", "1", "BoNhoGB", "1", "HinhAnhUrl", "/images/brands/a.jpg"));
            Check(c.Edit("P", "V", edit) is RedirectToRouteResult && v.TrangThai && v.SKU == "UPDATED" && v.MauSac == "Blue"
                && v.RAMGB == 1 && v.BoNhoGB == 1, "edit SKU attributes, reactivate");
            var list = (IEnumerable<ProductVariantViewModel>)((ViewResult)c.Index("Q")).Model;
            Check(list.Count() == 3 && list.All(x => x.MaBienSanPham != "V") && list.Any(x => !x.TrangThai), "variant list scoped to parent including inactive");
            Check(db.StockAccesses == 0 && db.Stocks.Count() == 1 && v.TONKHO.SoLuongTon == 7, "no stock query or creation");
            db.SaveError = new DbUpdateException("SQL detail must not leak");
            edit = Bind<ProductVariantEditViewModel>(c, Data(true, "SKU", "SAFE"));
            Check(c.Edit("P", "V", edit) is ViewResult && c.ModelState[""].Errors.Count > 0 && !c.ModelState[""].Errors[0].ErrorMessage.Contains("SQL detail"), "variant save safe error");
            model = Bind<ProductVariantViewModel>(c, Data(true, "MaBienSanPham", "FAIL", "SKU", "FAIL"));
            Check(c.Create("P", model) is ViewResult && c.ModelState[""].Errors.Count > 0, "variant create save failure");
            db.SaveError = new DbUpdateConcurrencyException("fixture deleted row");
            edit = Bind<ProductVariantEditViewModel>(c, Data(true, "SKU", "SAFE"));
            Check(c.Edit("P", "V", edit) is ViewResult && c.ModelState[""].Errors.Count > 0, "variant concurrent disappearance");
        }
        foreach (var culture in new[] { CultureInfo.InvariantCulture, new CultureInfo("vi-VN") })
        {
            var cultureDb = Seed();
            using (var c = Controller<ProductVariantsController>(cultureDb))
            {
                var model = BindCulture<ProductVariantViewModel>(c, culture, Data(true, "GiaNhap", 12.34m.ToString(culture), "GiaBan", 56.78m.ToString(culture)));
                Check(c.Create("P", model) is RedirectToRouteResult && cultureDb.Variants.Find("VNEW").GiaNhap == 12.34m
                    && cultureDb.Variants.Find("VNEW").GiaBan == 56.78m, "MVC decimal binding culture " + culture.Name);
            }
        }
    }

    static void CompileViews(string project)
    {
        foreach (var folder in new[] { "Products", "ProductVariants" })
        foreach (var file in Directory.GetFiles(Path.Combine(project, "Areas/Admin/Views", folder), "*.cshtml"))
        {
            var host = new MvcWebPageRazorHost("~/Areas/Admin/Views/" + folder + "/" + Path.GetFileName(file), file);
            foreach (var ns in new[] { "System", "System.Linq", "System.Collections.Generic", "System.Web.Mvc", "System.Web.Mvc.Html" }) host.NamespaceImports.Add(ns);
            using (var reader = File.OpenText(file))
            using (var compiler = new CSharpCodeProvider())
            {
                var generated = new RazorTemplateEngine(host).GenerateCode(reader);
                Check(generated.Success, "Razor parse " + folder + "/" + Path.GetFileName(file));
                var options = new CompilerParameters { GenerateInMemory = true };
                foreach (var assembly in new[] { "System.dll", "System.Core.dll", "Microsoft.CSharp.dll", "System.Web.dll", "System.ComponentModel.DataAnnotations.dll" }) options.ReferencedAssemblies.Add(assembly);
                foreach (var assembly in new[] { "QuanLiDienThoaiDD.dll", "System.Web.Mvc.dll", "System.Web.WebPages.dll", "System.Web.WebPages.Razor.dll", "System.Web.Razor.dll", "System.Web.Helpers.dll" }) options.ReferencedAssemblies.Add(Path.Combine(project, "bin", assembly));
                var result = compiler.CompileAssemblyFromDom(options, generated.GeneratedCode);
                if (result.Errors.HasErrors) throw new Exception(string.Join(Environment.NewLine, result.Errors.Cast<CompilerError>().Select(x => x.ToString())));
                Check(!result.Errors.HasErrors, "Razor compile " + folder + "/" + Path.GetFileName(file));
            }
        }
    }

    public static void Main(string[] args)
    {
        ProductValidation(); Products(); VariantValidation(); Variants();
        foreach (var type in new[] { typeof(ProductsController), typeof(ProductVariantsController) })
        {
            var auth = (AuthorizeAttribute)Attribute.GetCustomAttribute(type, typeof(AuthorizeAttribute));
            Check(auth != null && auth.Roles == "ADMIN", type.Name + " ADMIN");
            var posts = type.GetMethods().Where(x => Attribute.IsDefined(x, typeof(HttpPostAttribute))).ToArray();
            Check(posts.Length == 2 && posts.All(x => Attribute.IsDefined(x, typeof(ValidateAntiForgeryTokenAttribute))), type.Name + " POST anti-forgery");
            Check(!type.GetMethods().Any(x => x.Name == "Delete"), type.Name + " no delete");
        }
        CompileViews(args[0]);
        Console.WriteLine("Completed " + checks + " database-free checks. SQL, persistence, HTTP auth/CSRF, UI and concurrency remain UNVERIFIED.");
    }
}

class TestHttpContext : HttpContextBase
{
    readonly System.Collections.IDictionary items = new System.Collections.Hashtable();
    public override System.Collections.IDictionary Items { get { return items; } }
}

class FakeContext : QLHANGHOA_PHUKIEN_WEBEntities
{
    public readonly MemorySet<SANPHAM> Products = new MemorySet<SANPHAM>(x => x.MaSanPham);
    public readonly MemorySet<BIENSANPHAM> Variants = new MemorySet<BIENSANPHAM>(x => x.MaBienSanPham);
    public readonly MemorySet<LOAISANPHAM> Categories = new MemorySet<LOAISANPHAM>(x => x.MaLoai);
    public readonly MemorySet<THUONGHIEU> Brands = new MemorySet<THUONGHIEU>(x => x.MaThuongHieu);
    public readonly MemorySet<NHACUNGCAP> Suppliers = new MemorySet<NHACUNGCAP>(x => x.MaNCC);
    public readonly MemorySet<TONKHO> Stocks = new MemorySet<TONKHO>(x => x.MaBienSanPham);
    public int Saves, StockAccesses;
    public Exception SaveError;
    public override DbSet<SANPHAM> SANPHAMs { get { return Products; } set { } }
    public override DbSet<BIENSANPHAM> BIENSANPHAMs { get { return Variants; } set { } }
    public override DbSet<LOAISANPHAM> LOAISANPHAMs { get { return Categories; } set { } }
    public override DbSet<THUONGHIEU> THUONGHIEUx { get { return Brands; } set { } }
    public override DbSet<NHACUNGCAP> NHACUNGCAPs { get { return Suppliers; } set { } }
    public override DbSet<TONKHO> TONKHOes { get { StockAccesses++; return Stocks; } set { } }
    public override int SaveChanges() { if (SaveError != null) throw SaveError; Saves++; return 1; }
}

class MemorySet<T> : DbSet<T>, IQueryable<T> where T : class
{
    readonly List<T> rows = new List<T>();
    readonly Func<T, string> key;
    public MemorySet(Func<T, string> keySelector) { key = keySelector; }
    IQueryable<T> Query { get { return rows.AsQueryable(); } }
    public override T Add(T entity) { rows.Add(entity); return entity; }
    public override T Find(params object[] keys) { return rows.SingleOrDefault(x => key(x) == (string)keys[0]); }
    Type IQueryable.ElementType { get { return Query.ElementType; } }
    Expression IQueryable.Expression { get { return Query.Expression; } }
    IQueryProvider IQueryable.Provider { get { return Query.Provider; } }
    public IEnumerator<T> GetEnumerator() { return rows.GetEnumerator(); }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return rows.GetEnumerator(); }
}

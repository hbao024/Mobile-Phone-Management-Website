# Bàn giao CAT-02 — Quản lý thương hiệu

Snapshot ngày 2026-11-18, múi giờ Asia/Saigon. Quy trình khôi phục tại [AGENTS.md](../AGENTS.md); phạm vi được duyệt tại D-012 trong [decisions.md](decisions.md).

## Snapshot Git

| Đối tượng | Giá trị lúc kiểm tra trước commit CAT-02 |
|---|---|
| Branch | `feature/dat/catalog-brands` |
| Base / HEAD trước commit | `ff4736981e242df1f7e67df1e143b1383d3d0d88` |
| Remote `integration/foundation` | Cùng SHA trên, đã xác minh bằng `git ls-remote --heads origin` và fetch trước khi tạo branch; kiểm tra lại trước commit vẫn khớp |
| Branch CAT-01 lúc bắt đầu | `feature/dat/catalog-categories` / `a4a44ce1de96952ac5615060fa2d5efa0695587f`, remote khớp |
| CAT-01 | [Draft PR #1](https://github.com/hbao024/Mobile-Phone-Management-Website/pull/1), open, draft, chưa merge; đã đọc lại metadata GitHub |
| Công việc có sẵn/ngoài phạm vi | `foundation-review.patch` untracked có sẵn; `.vscode/settings.json` untracked xuất hiện trong phiên, không do CAT-02 tạo; giữ nguyên, không stage/commit |
| Base PR CAT-02 | `integration/foundation` |
| Commit triển khai / HEAD trước cập nhật bàn giao | `4b8731b491b616fc89ede70b9b5acf4ed89f7c13`, đã push và xác minh remote khớp |
| Draft PR CAT-02 | [#2](https://github.com/hbao024/Mobile-Phone-Management-Website/pull/2), open, draft; base/head refs và SHA khớp lúc tạo |

Lúc bắt đầu, working tree/staging sạch ngoài patch có sẵn. Snapshot CAT-01 mô tả HEAD trước cập nhật tài liệu; đã đọc commit `a4a44ce` chỉ sửa HANDOFF. Tạo branch từ nền đã fetch, không cherry-pick/merge CAT-01. Chỉ tham khảo tổ chức controller/ViewModels/views và harness bằng `git show`; file ứng dụng/test CAT-02 có tên/đường dẫn riêng, không phụ thuộc CAT-01. Không reset hoặc sửa patch/editor settings.

SHA của commit chứa tài liệu này phải đọc lại từ Git; không tự gán SHA cho chính commit đang chuẩn bị.

## Nội dung triển khai

- Truy cập trực tiếp `/Admin/Brands` với role `ADMIN`, dùng layout Admin hiện có, không thêm menu chung.
- Danh sách gồm mã, tên, mô tả, đường dẫn logo và cả hai trạng thái. Razor encode dữ liệu; logo hiển thị dạng văn bản, không tải/đọc/preview ảnh.
- Tạo mã/tên/mô tả/logo/trạng thái; form mới mặc định hoạt động. Sửa tên/mô tả/logo/trạng thái; mã giữ bất biến. Bỏ chọn “Đang sử dụng” để ngừng sử dụng; không có endpoint xóa cứng hoặc sửa quan hệ sản phẩm.
- ViewModel riêng chống overposting; hai POST có Anti-Forgery Token. Validation bắt buộc mã/tên, giới hạn SQL/EDMX 10/100/500/500; mã mới dùng chữ ASCII/số/`_`/`-`. Trim đầu/cuối trước lưu; mô tả/logo trống lưu null.
- `LogoUrl` tùy chọn: HTTP/HTTPS không kèm thông tin đăng nhập, hoặc đường dẫn `/images/...`, `~/images/...`; chặn scheme khác, protocol-relative, backslash, ký tự trắng/control thô, đường dẫn lùi và control mã hóa ở đường dẫn nội bộ. Không kiểm tra ảnh có tồn tại/nội dung/MIME vì không truy cập ảnh hay upload.
- Kiểm mã/tên trùng bằng truy vấn EF để SQL áp dụng collation; bao gồm thương hiệu ngừng sử dụng. Loại trừ chính bản ghi khi sửa. Unique constraint SQL vẫn quyết định lúc tranh chấp; bắt lỗi unique 2601/2627 và lỗi lưu EF, trả thông báo an toàn. Không tìm thấy bản ghi trả 404.
- Dùng entity `THUONGHIEU` và DbSet generated `THUONGHIEUx` trong context hiện có. Không sửa entity/context/EDMX, SQL, seed, dependency, startup, auth, storefront hoặc layout.
- `.csproj` thuộc Hoàng Bảo điều phối: chỉ thêm đăng ký hai C# và bốn Razor view. Harness riêng `tests/CAT-02` dùng lại cách kiểm CAT-01, không thêm framework/package hoặc sửa code ứng dụng để tiêm context.

## Bằng chứng kiểm chứng

Chạy tại repository root:

```powershell
$msbuild = 'C:/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Current/Bin/MSBuild.exe'
& $msbuild DoAnLTWeb/QuanLiDienThoaiDD/QuanLiDienThoaiDD.sln /t:Restore /p:RestorePackagesConfig=true /v:minimal /nologo
& $msbuild DoAnLTWeb/QuanLiDienThoaiDD/QuanLiDienThoaiDD.sln /t:Build /p:Configuration=Debug /v:minimal /nologo
powershell -NoProfile -ExecutionPolicy Bypass -File tests/CAT-02/Run.ps1
git diff --check
```

| Lớp kiểm chứng | Kết quả thực tế |
|---|---|
| Restore | Thành công bằng packages.config hiện có; không thêm dependency |
| Build Debug / .NET Framework 4.7.2 | Thành công bằng VS MSBuild; cảnh báo MSB3247 version binding hiện hữu cho Microsoft.Web.Infrastructure, Newtonsoft.Json, WebGrease, Antlr3.Runtime; không sửa config chung |
| Harness dữ liệu giả | 61 kiểm tra thành công, không chạy Application_Start hoặc SQL; dùng subclass context/DbSet giả trong tiến trình test |
| Binding/validation và controller | Tạo/sửa, thiếu/trắng tên, thiếu/sai/trùng mã, tên trùng thương hiệu ngừng sử dụng, vượt độ dài/đúng biên, URL hợp lệ/không hợp lệ, xóa logo về null, GET/POST bản ghi không tồn tại, đọc form sửa, hai chiều trạng thái, giữ mã và quan hệ khi overpost, danh sách hai trạng thái, thông báo thành công/lỗi lưu/lỗi mất bản ghi lúc lưu |
| Metadata và Razor | ADMIN, hai POST có anti-forgery, không Delete; parse/compile độc lập cả bốn Razor view trong 61 kiểm tra; chưa render HTTP/UI |
| Diff/staging | Đúng 11 file CAT-02; diff/check và cached/check thành công; patch và `.vscode/` không thuộc commit. Cập nhật bàn giao sau PR khôi phục BOM gốc của .csproj để diff cuối chỉ còn sáu đăng ký file |
| UI, HTTP authentication/CSRF | CHƯA CHẠY; reflection metadata không chứng minh middleware/filter chạy đúng qua HTTP |
| SQL/persistence/collation/unique race/concurrency | CHƯA CHẠY; LINQ to Objects và SaveChanges giả không mô phỏng SQL collation, constraint, transaction hay lưu bền |

## Giới hạn và điểm dừng

- Chưa có database kiểm thử được chỉ định. Không chạy SQL, seed hoặc khởi động ứng dụng: project thiếu Web.config gốc, startup hiện gọi `SeedData.EnsureDemoPasswords()`.
- Không có concurrency token trong schema: hai người sửa cùng thương hiệu có thể ghi đè nhau; catch concurrency chỉ xử lý trường hợp EF phát hiện như bản ghi biến mất khi lưu. Không khẳng định đã xử lý lost update.
- Storefront hiện lọc trạng thái sản phẩm, chưa lọc `THUONGHIEU.TrangThai`. Ngừng sử dụng trong Admin chưa tự ẩn sản phẩm tại cửa hàng; phần storefront thuộc phạm vi khác.
- Khi tích hợp sau review, `.csproj`, HANDOFF và decisions là file dùng chung với CAT-01, cần giữ cả hai gói khi giải quyết diff; không lấy nguyên file từ một branch đè branch còn lại.
- Đã commit/push và tạo Draft PR #2 theo D-012. GitHub connector tạo PR bị 403; đã hoàn tất qua GitHub API bằng credential Git hiện có, không lưu credential vào file hoặc đầu ra. Cập nhật bàn giao và khôi phục BOM .csproj được commit/push riêng; SHA cuối đọc lại từ Git/PR. Dừng để ChatGPT review, không merge/Ready CAT-01 hoặc CAT-02, không bắt đầu gói tiếp theo.

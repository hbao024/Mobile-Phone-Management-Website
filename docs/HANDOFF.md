# Bàn giao CAT-01 — Quản lý loại sản phẩm

Snapshot ngày 2026-11-18, múi giờ Asia/Saigon. Quy trình khôi phục context tại [AGENTS.md](../AGENTS.md); quyền triển khai phiên này tại D-011 trong [decisions.md](decisions.md).

## Snapshot Git

| Đối tượng | Giá trị lúc kiểm tra trước commit CAT-01 |
|---|---|
| Branch làm việc | `feature/dat/catalog-categories` |
| Base / HEAD trước commit CAT-01 | `ff4736981e242df1f7e67df1e143b1383d3d0d88` |
| Base PR | `integration/foundation` |
| Remote `integration/foundation` | `ff4736981e242df1f7e67df1e143b1383d3d0d88`, đã xác minh bằng `git ls-remote --heads origin` |
| Nguồn Nhàn `origin/NguyenThanhNhan` | `a675fdcf712fcdb52f1d3a562c2a17857ee0850c`, remote đã xác minh |
| Nguồn Tân `origin/DaoDuyTan` | `4990488c1e85dacdf762eab817f3cceeb183171c`, remote đã xác minh; Admin Tân chưa tích hợp |
| Công việc có sẵn | `foundation-review.patch` untracked trước khi tạo branch; giữ nguyên, không stage/commit |

Branch CAT-01 chưa tồn tại local/remote lúc kiểm tra, đã tạo từ đúng SHA trên. Snapshot cũ mô tả HEAD trước commit tài liệu; đã đọc commit `ff47369` và xác nhận chỉ thêm năm file tài liệu so với nguồn Nhàn. Không reset hoặc ghi đè branch. SHA commit CAT-01 và trạng thái sau push phải đọc lại từ Git/PR, không gán trước SHA của commit đang chứa file này.

## Nội dung và phạm vi

- Truy cập trực tiếp `/Admin/Categories` bằng tài khoản có role `ADMIN`; không sửa menu/layout chung.
- Danh sách hiển thị mã, tên, mô tả và cả hai trạng thái. Không cần trang chi tiết riêng vì danh sách và form sửa đã hiển thị đủ bốn trường.
- Thêm mã/tên/mô tả/trạng thái; sửa tên/mô tả/trạng thái, giữ mã bất biến. Không có endpoint xóa cứng; trạng thái chỉ đổi qua POST Edit.
- ViewModel kiểm tra bắt buộc, độ dài theo SQL/EDMX (10/100/500), mã mới gồm chữ ASCII/số/`_`/`-`. Trim đầu/cuối; kiểm mã/tên trùng bao gồm loại ngừng sử dụng theo collation database. Unique constraint vẫn quyết định khi có yêu cầu đồng thời; lỗi lưu trả thông báo an toàn, không xuất chi tiết SQL.
- Dùng lại `[Authorize(Roles = "ADMIN")]`, EF6 context/entity hiện có, Anti-Forgery Token ở cả hai POST. Edit ViewModel không chứa mã hoặc navigation properties.
- `.csproj` do Hoàng Bảo điều phối: chỉ đăng ký hai file C# và bốn Razor view mới để compile/package; không đổi dependency/config.
- Thêm `tests/CAT-01/CategoryChecks.cs` và `Run.ps1` làm harness kiểm chứng độc lập. Dùng subclass context và DbSet giả chỉ trong tiến trình test, không thêm context vào ứng dụng, không gọi startup hoặc database.
- Không sửa entity/EDMX, SQL, seed, config, authentication, storefront Nhàn hoặc đơn hàng/nhập hàng Tân. Không triển khai các đề xuất tồn kho/concurrency chưa duyệt.

## Bằng chứng kiểm chứng

MSBuild sử dụng: `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe`.

```powershell
# Chạy tại repository root, đặt $msbuild theo đường dẫn ở trên.
& $msbuild DoAnLTWeb/QuanLiDienThoaiDD/QuanLiDienThoaiDD.sln /t:Restore /p:RestorePackagesConfig=true /v:minimal /nologo
& $msbuild DoAnLTWeb/QuanLiDienThoaiDD/QuanLiDienThoaiDD.sln /t:Build /p:Configuration=Debug /v:minimal /nologo
powershell -NoProfile -ExecutionPolicy Bypass -File tests/CAT-01/Run.ps1
git diff --check
```

| Lớp kiểm chứng | Kết quả thực tế |
|---|---|
| Dependency restore | Thành công, dùng đúng `packages.config`, không thêm package |
| Build solution Debug / .NET Framework 4.7.2 | Thành công; cảnh báo MSB3247 về version binding hiện hữu (`Microsoft.Web.Infrastructure`, `Newtonsoft.Json`, `WebGrease`, `Antlr3.Runtime`); chưa sửa config chung |
| Harness không database | 31 kiểm tra thành công: MVC model binding/validation, thiếu/trắng tên, thiếu/sai/trùng mã, trùng tên loại ngừng sử dụng, vượt độ dài và đúng biên, thêm, sửa trùng/thiếu tên, GET/POST bản ghi không tồn tại, đổi trạng thái hai chiều, giữ mã/quan hệ khi overpost, thông báo thành công/lỗi lưu/lỗi bản ghi mất khi lưu, danh sách hai trạng thái, metadata ADMIN/POST/anti-forgery, không có Delete |
| Razor view | Cả bốn view đã parse và compile độc lập bằng MVC Razor host trong harness; tính trong 31 kiểm tra, không phải render HTTP/UI |
| Diff và staging | Kiểm phạm vi 11 file CAT-01, `git diff --check` và `git diff --cached --check`; file patch có sẵn không thuộc staging |
| UI, HTTP auth/CSRF, SQL, persistence, collation và concurrent unique race | CHƯA CHẠY; harness giả không chứng minh các hành vi này trên môi trường thật |

## Giới hạn và điểm dừng

- Chưa có database kiểm thử được chỉ định; không tạo/chạy SQL, seed hoặc ghi dữ liệu thật. Project thiếu `Web.config` gốc; `Application_Start` gọi `SeedData.EnsureDemoPasswords()`, vì vậy không khởi động ứng dụng trong phiên này.
- Storefront hiện chưa lọc `LOAISANPHAM.TrangThai`; ngừng sử dụng trong CAT-01 không tự ẩn sản phẩm/loại ở cửa hàng. Cần phối hợp Nhàn trong phạm vi được duyệt riêng.
- Schema chưa có concurrency token: sửa cùng bản ghi có thể ghi đè thay đổi của nhau; catch concurrency hiện chỉ xử lý lỗi EF phát hiện được như bản ghi bị xóa trước SaveChanges. Chưa có kiểm chứng đồng thời với SQL.
- Commit/push branch và tạo Draft PR vào `integration/foundation` theo D-011. Kết quả cuối cùng xem Git và link PR trong báo cáo phiên. Không merge, không Ready, không CAT-02; chờ review CAT-01.

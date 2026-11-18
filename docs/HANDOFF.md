# Bàn giao — CAT-03 / NgoThanhDat

Snapshot 2026-11-18 (Asia/Saigon). Quy trình: [AGENTS.md](../AGENTS.md); quyền triển khai CAT-03 tại D-014 trong [decisions.md](decisions.md). Dừng review trước quản lý sản phẩm.

## Git

- Branch duy nhất: `NgoThanhDat`; HEAD trước CAT-03: `f2c0bd6161e5f65c0e44a23b9059b52e274c0541`, local/remote khớp lúc bắt đầu. Đã đọc commit bàn giao này, chỉ sửa HANDOFF so với commit tổng hợp code `174e62d`.
- Không tạo branch/PR, không merge/force-push; main, integration/foundation và branch Nhàn/Tân giữ nguyên. PR #1/#2 đã đóng từ CONSOLIDATE-DAT.
- Working tree ban đầu sạch ngoài `foundation-review.patch`, `.vscode/settings.json` untracked. Giữ nguyên nội dung và không đưa vào commit.
- SHA CAT-03 sau commit/push đọc lại từ Git; không tự gán SHA cho commit đang chứa file này.

## Nội dung và phạm vi

- `/Admin/Suppliers`: danh sách mã/tên/địa chỉ/điện thoại/email và cả hai trạng thái; thêm/sửa, ngừng sử dụng qua form sửa; thông báo thành công và lỗi lưu, bản ghi không tồn tại trả 404. Không có xóa cứng.
- Dùng `NHACUNGCAP` và `NHACUNGCAPs` của context hiện có; ADMIN, hai POST có anti-forgery, ViewModel chỉ nhận scalar, mã bất biến khi sửa; không sửa quan hệ SANPHAM/PHIEUNHAP.
- SQL/EDMX: mã bắt buộc varchar(10), tên bắt buộc nvarchar(150), địa chỉ tùy chọn nvarchar(300), điện thoại tùy chọn varchar(15), email tùy chọn varchar(255). Chỉ kiểm trùng mã bằng truy vấn SQL/EF; tên, điện thoại, email được phép trùng.
- Mã dùng chữ ASCII/số/_/-. Điện thoại dùng PhoneAttribute cùng ký tự số ASCII và dấu định dạng thông dụng; email dùng EmailAddressAttribute và ASCII để tránh mất ký tự ở cột varchar. Trường liên hệ trống lưu null. Không áp thêm unique hoặc thay schema.
- Thêm controller, ViewModels và bốn Razor view riêng; `.csproj` (Hoàng Bảo điều phối) chỉ thêm sáu đăng ký cần compile/package. Thêm harness `tests/CAT-03` theo cách CAT-01/CAT-02, không thêm dependency/framework.
- Không sửa CAT-01/CAT-02, SQL/EDMX/entity/context generated, auth, storefront, layout chung hoặc module Tân. Sổ quyết định chỉ bổ sung quyền CAT-03; không thêm tài liệu mới.

## Kiểm chứng

- Build Debug bằng `C:/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Current/Bin/MSBuild.exe` với solution `DoAnLTWeb/QuanLiDienThoaiDD/QuanLiDienThoaiDD.sln`: thành công; cảnh báo MSB3247 dependency binding hiện hữu, không sửa config.
- `powershell -NoProfile -ExecutionPolicy Bypass -File tests/CAT-03/Run.ps1`: **64/64 đạt**. Kiểm binding/validation Create/Edit, năm giới hạn độ dài và đúng biên, mã trùng/không hợp lệ, tên/liên hệ trùng được phép, optional/null, không tồn tại, hai chiều trạng thái, overposting mã/sản phẩm/phiếu nhập, thông báo/lỗi SaveChanges/lỗi concurrency giả; parse/compile bốn view.
- Chạy lại harness CAT-01 **31/31**, CAT-02 **80/80** đạt. Tất cả là dữ liệu giả/metadata và compile Razor, chưa chứng minh runtime HTTP hay SQL persistence/collation/unique race/concurrency.
- Không có database kiểm thử an toàn được chỉ định; project vẫn thiếu Web.config gốc, startup gọi `SeedData.EnsureDemoPasswords()`, nên không khởi động app hoặc chạy SQL/seed. UI, HTTP ADMIN/CSRF và SQL chưa kiểm chứng.
- Schema không có concurrency token nên sửa đồng thời có thể ghi đè; catch chỉ xử lý lỗi EF phát hiện. CAT-03 chỉ quản lý trạng thái nhà cung cấp, không đổi luồng chọn nhà cung cấp ở module khác.
- Kiểm diff/staging trước commit, đúng 11 file CAT-03; giữ nguyên untracked. Commit/push vào `NgoThanhDat` rồi dừng để ChatGPT review, chưa làm quản lý sản phẩm.

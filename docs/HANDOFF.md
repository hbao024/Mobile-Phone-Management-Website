# Bàn giao — NgoThanhDat

Snapshot 2026-11-18 (Asia/Saigon). Quy trình: [AGENTS.md](../AGENTS.md); quyền hợp nhất: D-013 trong [decisions.md](decisions.md). Chỉ thống nhất CAT-01/CAT-02, chưa triển khai chức năng tiếp theo.

## Git và nguồn

| Nội dung | Trạng thái lúc kiểm tra trước commit tổng hợp |
|---|---|
| Branch làm việc chính | `NgoThanhDat`; hợp nhất trên branch tạm `feature/dat/catalog-management` từ `integration/foundation`, sau đó đổi tên an toàn |
| Base / HEAD trước commit | `ff4736981e242df1f7e67df1e143b1383d3d0d88`, remote đã fetch/xác minh |
| CAT-01 / PR #1 | `a4a44ce1de96952ac5615060fa2d5efa0695587f` |
| CAT-02 / PR #2 | `618927ed40b0c0df3275e8dc3c4d7fab298eb959`, gồm Review Fix 01 |
| PR / branch cũ | Theo điều chỉnh mới nhất: không tạo PR mới; xác minh remote `NgoThanhDat` đủ nội dung trước khi đóng PR #1/#2 và xóa ba branch cũ |
| Ngoài phạm vi | `foundation-review.patch`, `.vscode/settings.json` untracked có sẵn, giữ nguyên và không commit |

SHA commit tổng hợp đọc lại từ Git; không tự gán SHA cho commit đang chứa file này. Giữ nguyên `integration/foundation`, `main`, `NguyenThanhNhan`, `DaoDuyTan`. Không merge/force-push. Chỉ dùng `NgoThanhDat` cho công việc Đạt tiếp theo.

## Nội dung và bảo toàn

- `/Admin/Categories` và `/Admin/Brands`: danh sách hai trạng thái, thêm/sửa, ngừng sử dụng; ADMIN, ViewModel, POST/anti-forgery và validation như code đã review.
- Nhập nguyên nội dung 16 file code/views/tests từ đúng hai HEAD nguồn, không sửa nghiệp vụ; đối chiếu Git blob từng file. Chỉ hợp nhất `.csproj` (đủ 4 C# và 8 Razor view), D-011/D-012, bổ sung D-013 và thay HANDOFF.
- Diff so với nền chỉ gồm 19 file thuộc hai gói và tài liệu; không xóa file, không đổi SQL/schema/EDMX/context, authentication, storefront, layout hoặc dependency. Không chạy database/seed.

## Kiểm chứng và giới hạn

- Build Debug bằng VS MSBuild/.NET Framework 4.7.2 thành công; còn cảnh báo MSB3247 dependency binding hiện hữu.
- `powershell -NoProfile -ExecutionPolicy Bypass -File tests/CAT-01/Run.ps1`: **31/31 đạt**.
- `powershell -NoProfile -ExecutionPolicy Bypass -File tests/CAT-02/Run.ps1`: **80/80 đạt**, gồm giới hạn logo trong /images/ hoặc ~/images/.
- Hai harness dùng dữ liệu giả; đã parse/compile cả 8 Razor view. Chưa kiểm chứng UI/HTTP auth/CSRF, SQL persistence/collation/unique race hoặc concurrency.
- Chưa có DB kiểm thử được chỉ định; project thiếu Web.config gốc và startup gọi seed nên không khởi động ứng dụng. Schema chưa có concurrency token; sửa đồng thời có thể ghi đè. Ngừng sử dụng chưa tự ẩn sản phẩm ở storefront.
- Dừng sau push/xác minh `NgoThanhDat`, đóng hai PR cũ với liên kết branch mới và xóa các branch cũ đã được bảo toàn. Không tạo PR mới, không merge, không tự làm CAT-03.

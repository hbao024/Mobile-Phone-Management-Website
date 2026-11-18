# Bàn giao — CAT-04 / NgoThanhDat

Snapshot 2026-11-18 (Asia/Saigon). Quy trình: [AGENTS.md](../AGENTS.md); phạm vi tại D-015 trong [decisions.md](decisions.md), chi tiết tại [contract](contracts/catalog-inventory.md#biên-quản-lý-catalog-cat-04). Dừng để ChatGPT review.

## Git và phạm vi

- Làm trên `NgoThanhDat`; HEAD trước CAT-04 `fea07e46b7554ea3f2db6cee5a444bff7ed15bfb`, local/remote khớp lúc bắt đầu. Snapshot CAT-03 mô tả HEAD trước commit; đã đối chiếu commit CAT-03 thực tế và source trước tiếp tục.
- Không tạo branch/PR, merge, force-push hoặc sửa main/nền/branch Nhàn/Tân. `foundation-review.patch` và `.vscode/settings.json` untracked có sẵn được giữ nguyên, không commit.
- Hai controller, một file ViewModels, tám Razor view riêng cho Products/ProductVariants, hai file harness CAT-04. File chung `.csproj` chỉ thêm 3 C# + 8 view để compile/package (Hoàng Bảo điều phối); contract ghi biên ảnh/liên kết/giá/tồn để không nhập nhằng với đề xuất chưa duyệt; decisions ghi D-015, HANDOFF cập nhật phiên này.
- Không sửa CAT-01/02/03, SQL/schema/EDMX/entity/context generated, authentication, storefront, layout, module Tân hoặc dependency. SHA CAT-04 sau commit/push đọc lại từ Git, không tự gán SHA cho commit chứa file này.

## Chức năng

- `/Admin/Products`: danh sách hai trạng thái, thêm/sửa tên/mô tả/loại/thương hiệu/NCC mặc định/bảo hành/trạng thái. Mã sản phẩm bất biến; không xóa cứng, không đổi dây chuyền trạng thái biến thể.
- Chọn danh mục đang sử dụng; sửa được giữ nguyên liên kết cũ ngừng sử dụng, không chuyển sang danh mục ngừng sử dụng khác. NCC mặc định tùy chọn; form ghi rõ liên kết cũ.
- `/Admin/ProductVariants?productId=...`: danh sách/thêm/sửa biến thể theo sản phẩm, kiểm quyền sở hữu sản phẩm–biến thể khi GET/POST. Mã và cha bất biến; SKU sửa được, duy nhất toàn bảng gồm biến thể ngừng bán. Không áp unique tên hoặc tổ hợp cấu hình.
- ViewModel, ADMIN, cả bốn POST có anti-forgery. Validation PK/độ dài/FK, bảo hành 0–120, RAM/bộ nhớ tùy chọn 1–32767, giá không âm và đúng decimal(18,2), tối đa 9999999999999999.99. Không âm thầm làm tròn giá có hơn 2 chữ số thập phân ở giá trị decimal nhận vào.
- Ảnh tùy chọn qua `HinhAnhUrl`, tối đa 500; chỉ nhận /images/... với ký tự tên an toàn để tương thích storefront đang ghép ~, chặn traversal, dấu nháy/backslash/control/URL ngoài; không upload/truy cập ảnh.
- Chỉ cập nhật scalar catalog. Không tạo/cập nhật TONKHO, nhật ký kho, quan hệ/chứng từ/giỏ hàng hoặc snapshot giá/tên. Biến thể mới chưa có TONKHO; việc chuẩn bị tồn thuộc luồng khác, chưa nghiệm thu.

## Kiểm chứng và giới hạn

- VS MSBuild tại `C:/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Current/Bin/MSBuild.exe`, build Debug solution `DoAnLTWeb/QuanLiDienThoaiDD/QuanLiDienThoaiDD.sln`: thành công; cảnh báo MSB3247 binding hiện hữu, không sửa config.
- `powershell -NoProfile -ExecutionPolicy Bypass -File tests/CAT-04/Run.ps1`: **211 kiểm tra dữ liệu giả đạt**, gồm Create/Edit validation, PK/SKU trùng toàn bảng, FK cũ ngừng sử dụng, dữ liệu không tồn tại/sai cha, biên giá/độ dài/RAM/bảo hành, trạng thái, chống overposting khóa/ngày/cha/stock/chứng từ/giỏ, lỗi lưu/concurrency giả, binding số invariant/vi-VN và parse/compile tám view.
- Chạy hồi quy CAT-01 **31/31**, CAT-02 **80/80**, CAT-03 **64/64** đạt. Harness chỉ kiểm metadata ADMIN/POST/anti-forgery, không chứng minh filter chạy qua HTTP.
- Không có database kiểm thử an toàn được chỉ định, project thiếu Web.config gốc và startup gọi seed: không khởi động app, chạy SQL hoặc seed. UI/HTTP auth/CSRF, SQL persistence/collation/unique race, transaction và concurrency chưa nghiệm thu.
- Chưa có concurrency token: sửa đồng thời có thể ghi đè. Storefront/checkout chưa lọc đầy đủ trạng thái biến thể/danh mục; trạng thái Admin không bảo đảm đã chặn mua phía khách. Không sửa các giới hạn này trong CAT-04.
- Kiểm diff/staging đúng 17 file thuộc gói, không đổi module cũ; giữ file untracked. Commit/push trên `NgoThanhDat`, dừng review, không chuyển chức năng tiếp theo.

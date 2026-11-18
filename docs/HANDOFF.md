# Bàn giao — CAT-06 / NgoThanhDat

Snapshot 2026-11-18 (Asia/Saigon). Quy trình: [AGENTS.md](../AGENTS.md); phạm vi được duyệt tại D-017 trong [decisions.md](decisions.md). Dừng review, không tự chuyển CAT-07.

## Git và phạm vi

- Branch `NgoThanhDat`; HEAD trước CAT-06 `51e9c60f7bc88b184c9ff614db1153aa8802dc48`. Đã đọc commit CAT-05/source và đối chiếu snapshot bàn giao ghi SHA trước commit; local/remote khớp qua ls-remote lúc bắt đầu.
- Chỉ sửa ProductsController, Products/Index và thêm tests/CAT-06; cập nhật decisions/HANDOFF. Không sửa storefront Nhàn/module Tân, schema/EDMX/generated, kho, auth/layout, project/dependency hoặc service.
- Giữ nguyên `.vscode/settings.json` và `foundation-review.patch` untracked có sẵn, không commit. Phiên được giao commit/push branch hiện tại, không tạo branch/PR/merge. SHA commit chứa tài liệu này đọc lại từ Git và đối chiếu remote sau push.

## Hành vi

- GET Admin Products tìm chuỗi con theo mã/tên sản phẩm hoặc SKU (gồm biến thể ngừng bán), EXISTS tránh lặp sản phẩm. Lọc loại/thương hiệu kể cả danh mục ngừng dùng; trạng thái là trạng thái sản phẩm.
- Sắp xếp tên tăng/giảm, ngày tạo mới/cũ, mã; mã sản phẩm là khóa phụ ổn định. Count và Skip/Take trên IQueryable EF6, chỉ lấy 20 dòng/trang trước ToList; dropdown danh mục lấy riêng, không tải toàn bộ sản phẩm.
- Form GET áp dụng bộ lọc về trang đầu, bốn liên kết Đầu/Trước/Sau/Cuối giữ điều kiện. Hiển thị tổng/trang và kết quả rỗng; không dựng hàng nghìn link.
- Trang sai định dạng/âm/0/overflow về 1; vượt tổng trang về trang cuối; trạng thái/sort lạ về mặc định kèm thông báo. Filter mã không tồn tại cho kết quả rỗng và giữ lựa chọn; q >200, mã loại/thương hiệu >10 trả HTTP 400.
- Thứ tự có khóa phụ nhưng dữ liệu thay đổi giữa hai request hoặc giữa Count và lấy trang vẫn có thể đổi kết quả. Không thêm snapshot transaction. So khớp hoa/thường/dấu theo collation SQL, chưa xác minh trên DB thật.

## Kiểm chứng và giới hạn

- VS MSBuild Debug solution thành công; chỉ cảnh báo MSB3247 binding hiện hữu.
- `tests/CAT-06/Run.ps1`: **31/31**, gồm dữ liệu giả 45 sản phẩm, tìm mã/tên/SKU, chống lặp do nhiều SKU, lọc kết hợp, các sort/tie, trang đầu/giữa/cuối/rỗng/overflow, không ghi DB/đọc kho; kiểm tra source bốn link giữ tham số.
- Trong bộ trên, EF6 thực sự sinh SQL offline từ phương thức truy vấn production cho năm kiểu sort, có tìm SKU/filters/Skip/Take. Dùng EDMX hiện tại, tắt initializer và connection giả `invalid.invalid`; không thực thi SQL. Không coi sinh SQL là bằng chứng kết quả/performance/collation DB.
- Hồi quy thực chạy CAT-01 **31/31**, CAT-02 **80/80**, CAT-03 **64/64**, CAT-04 **211/211** (gồm parse/compile Razor Products/Index), CAT-05 **100/100** + script preview DOM giả đạt.
- Chưa có DB kiểm thử an toàn được chỉ định, Web.config gốc vẫn thiếu và startup gọi seed: không khởi động IIS/app, không chạy SQL/seed. UI/HTTP binding/auth/CSRF, SQL thực thi/query plan/collation và concurrency chưa nghiệm thu. Test metadata ADMIN không chứng minh filter HTTP.
- Giới hạn CAT-04/05 vẫn giữ: chưa concurrency token, storefront chưa lọc đầy đủ trạng thái, biến thể mới không tự tạo tồn; file ảnh cũ được giữ để bảo vệ tham chiếu, lỗi lưu không rõ có thể giữ file dư.

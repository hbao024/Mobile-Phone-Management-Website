# Bàn giao — CAT-07 điều chỉnh tồn / NgoThanhDat

Snapshot 2026-11-19 (Asia/Saigon). Quy trình [AGENTS.md](../AGENTS.md), phạm vi D-019 trong [decisions.md](decisions.md); chi tiết [contract](contracts/catalog-inventory.md#điều-chỉnh-kiểm-kê-và-inventoryservice-2026-11-19). Dừng để review.

## Git và file

- Branch NgoThanhDat; HEAD trước phiên `fc11d07ac2653d1ddc119aa3bf1fbc86f5b5d0fb`. Đã đọc commit xem tồn thực tế để đối chiếu snapshot ghi HEAD trước commit; local/remote cùng SHA lúc bắt đầu.
- DaoDuyTan remote/fetch vẫn `4990488c1e85dacdf762eab817f3cceeb183171c`; đã đọc lại Orders.ProcessStatusTransition/Create và StockReceipts.ApplyStockReceiptToInventory/điểm gọi, SQL/EDMX/model dùng chung. Không sửa/merge/push branch Tân hoặc code Nhàn.
- Đạt: thêm InventoryService, InventoryAdjustmentViewModel, Adjust/History views; sửa InventoryController/Index view. csproj (Bảo điều phối) chỉ đăng ký 2 C# + 2 view; cập nhật tests/CAT-07 và thêm AdjustmentChecks.cs. Cập nhật contract/decisions/HANDOFF và một câu kiến trúc về service mới. Không schema/EDMX/generated/dependency/config/seed.
- Giữ nguyên .vscode/settings.json và foundation-review.patch untracked có sẵn, không commit. Commit/push NgoThanhDat theo chỉ đạo rồi đối chiếu SHA remote trong kết quả phiên; không branch/PR/merge/force-push.

## Chức năng đã triển khai

- Giữ xem/tìm/lọc/phân trang tồn. Thêm form điều chỉnh tăng/giảm nguyên dương, lý do bắt buộc, ADMIN + POST/anti-forgery; actor lấy từ tài khoản/nhân viên active, route xác định biến thể. Không bind số tồn trước/sau hoặc DangDat.
- InventoryService nhận transaction từ caller, không tự commit; UPDATE có điều kiện tại SQL, OUTPUT trước/sau + ghi log trong transaction. Khóa range khi thiếu TONKHO; chỉ tạo khi tăng; giảm thiếu/âm và tăng tràn int bị từ chối, không sửa DangDat hiện hữu.
- Token thao tác server ký, app lock theo GUID và dấu trong GhiChu để nhận lại kết quả gửi lặp/khước từ payload khác. Chỉ chống lặp hợp tác qua service, chưa có unique operation key trong schema; không tuyên bố idempotency toàn hệ thống. Lỗi commit không rõ giữ token/nội dung, đối soát lịch sử, không tự retry bằng mã mới.
- History liệt kê giao dịch điều chỉnh gồm mã/ngày/loại/số lượng/trước/sau/NV/lý do, phân trang. Lỗi trả thông báo an toàn, không lộ SQL/connection.

## Kiểm chứng

- VS MSBuild Debug solution thành công; chỉ MSB3247 binding hiện hữu.
- tests/CAT-07/Run.ps1: **37** kiểm tra xem tồn/SQL sinh offline/Razor/metadata + **37** kiểm tra điều chỉnh (validation, token/actor controller, history, SQL source, ADO mô phỏng). Có tăng/giảm về 0, thiếu tồn/missing, số sai/overflow, log failure + outer rollback mô phỏng, replay/payload conflict. Không coi mock ADO là thực thi batch SQL hoặc test SQL transaction/concurrency.
- Hồi quy thực chạy CAT-01 **31/31**, CAT-02 **80/80**, CAT-03 **64/64**, CAT-04 **211/211**, CAT-05 **100/100** + preview DOM giả, CAT-06 **31/31**.

## Phối hợp và giới hạn

- Tân: chuyển đúng các điểm ghi trong contract sang service cùng transaction, xử lý EF tracked TONKHO, thống nhất DangDat giữa xác nhận thường/xác nhận ngay, trạng thái hoàn và chống áp lại theo chứng từ. API lõi có NHAP/BAN/TRA_HANG nhưng **chưa tích hợp/kiểm thử Tân**, không thay nghiệp vụ của Tân trong phiên này.
- Bảo: phối hợp cấu hình DB/IIS/machine key, mapping tài khoản–NV, quyền SQL/app lock, DB test an toàn; xem xét operation key/unique constraint nếu nhóm duyệt schema riêng. D-019 còn OPEN cho cơ chế liên module, không phê duyệt toàn bộ D-008/D-010.
- Chưa có DB test an toàn được chỉ định; thiếu Web.config gốc, startup gọi seed: không chạy app/IIS/SQL/seed. **Chưa nghiệm thu** batch SQL thực tế, hai context/transaction cạnh tranh, rollback/unknown commit SQL, UI/HTTP auth/CSRF và tích hợp liên module. Writer cũ Tân còn đọc-rồi-gán, nên chưa bảo đảm chống lost update khi cùng ghi. Xem contract về lock/replay scan, connection SET XACT_ABORT, transaction và schema.

## Session update — Admin product grid

- Updated `Areas/Admin/Views/Products/Index.cshtml`: filter controls now use a responsive CSS Grid, actions are grouped separately, and the product table has stable column behavior and horizontal overflow on narrow screens.
- Updated `Content/Site.css` with scoped product-page styles only; no schema, EDMX, authentication, or generated model changes.
- Added the approved warm chocolate theme in `Content/Site.css`: dark chocolate navbar/sidebar, SaddleBrown accent, ivory background/cards, beige borders, and warm CTA colors.
- Added dark-surface hover overrides so sidebar and navbar link text remains white and readable instead of inheriting the global dark hover color.
- Styled the Products page `Xem tồn kho` and `Xóa bộ lọc` links as matching warm outlined buttons.
- Filled the storefront `Home/About` page with a responsive introduction, values, metrics, and calls to action; added the `Liên hệ` navbar link beside `Giới thiệu`.
- Built the storefront `Home/Contact` page with store information, responsive contact form, anti-forgery validation, and success feedback through `ContactViewModel` and `HomeController`.
- Updated the contact address to `140 Lê Trọng Tấn, Tây Thạnh, TP. Hồ Chí Minh` and added a responsive Google Maps embed plus an external Maps link.
- Added shared button hover/active feedback: warm color transitions, lift effect, and shadow for clearer interaction.
- Restyled `Views/KhachHang/DangKy.cshtml` to reuse the login auth-card pattern, with responsive two-column registration fields, MobileHub branding, validation messages, and a login link; registration controller/model behavior was preserved.
- Adjusted the registration login prompt spacing so it sits lower and remains centered beneath the submit button.
- Centered the registration prompt with an explicit flex layout and compacted Login/Register auth spacing, card padding, and footer spacing to fit common viewport heights without unnecessary vertical scrolling.
- Verification: Debug solution build passed with Visual Studio 2022 MSBuild. Browser rendering and database runtime were not executed in this session.

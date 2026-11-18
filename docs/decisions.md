# Sổ quyết định

## Cách đọc

- **APPROVED**: được người dùng phê duyệt, không đồng nghĩa đã triển khai hoặc nghiệm thu runtime.
- **PROPOSED**: phương án kỹ thuật để review, chưa được phép triển khai chỉ nhờ có mặt trong tài liệu.
- **OPEN**: cần nhóm xác nhận chính sách hoặc trách nhiệm.

Nguồn duyệt hiện tại là chỉ đạo người dùng trong hội thoại FOUNDATION-01, FOUNDATION-02A, FOUNDATION-02B.1, CAT-01, CAT-02, CONSOLIDATE-DAT và CAT-03. Hai báo cáo đầu chưa được lưu thành artifact trong repository; không coi là tài liệu ký duyệt có sẵn. Mọi thay đổi trạng thái cần ghi căn cứ/người duyệt tại đây.

| Mã | Quyết định / liên kết nội dung | Trạng thái | Căn cứ và điều kiện xem xét lại |
|---|---|---|---|
| D-001 | Giữ stack hiện hữu tại [kiến trúc](architecture.md#stack-và-mục-tiêu) | APPROVED | Người dùng xác nhận từ FOUNDATION-01 và nhắc lại ở 02B.1; đổi stack cần review riêng |
| D-002 | Giữ mô hình ứng dụng và dữ liệu thống nhất tại kiến trúc | APPROVED | Chỉ đạo người dùng; không tự tách context hoặc tạo bộ EDMX thứ hai |
| D-003 | Dùng solution Nhàn làm ứng viên nền, kế thừa Admin Tân | APPROVED | Định hướng người dùng; chưa xác nhận tích hợp xong hoặc ứng viên đã chạy được |
| D-004 | Không tái cấu trúc lớn hoặc thêm service không cần thiết | APPROVED | Giới hạn người dùng; chỉ xem lại khi có nhu cầu cụ thể và phạm vi mới được duyệt |
| D-005 | Không đổi schema hoặc nghiệp vụ tồn kho trong gói hiện tại | APPROVED | Chỉ đạo FOUNDATION-02B.1; bản nháp contract không thay đổi giới hạn này |
| D-006 | Tạo branch nền và đúng năm file tài liệu theo [HANDOFF](HANDOFF.md) | APPROVED | Người dùng giao triển khai FOUNDATION-02B.1 theo kế hoạch đã review; commit tài liệu sau kiểm chứng theo kế hoạch đó, không merge/push hoặc bắt đầu bước tiếp theo |
| D-007 | Đưa solution/project về cấu trúc root | PROPOSED | Đề xuất FOUNDATION-02A; chưa thực hiện ở gói tài liệu |
| D-008 | Chính sách catalog, giá, giữ hàng và hoàn kho tại [contract](contracts/catalog-inventory.md#nội-dung-đề-xuất-cho-d-008) | OPEN | Nhóm cần chốt trước triển khai; chưa chọn giữ hàng, thời điểm giá hay điều kiện hoàn khi đang giao |
| D-009 | Ma trận quyền và ownership điều phối tại [kiến trúc](architecture.md#ownership-file-dùng-chung) | OPEN | Phân công chức năng có nguồn tham khảo; quyền nghiệp vụ và người điều phối cần xác nhận |
| D-010 | Concurrency, chống lặp và sinh mã tại [contract](contracts/catalog-inventory.md#nội-dung-đề-xuất-cho-d-010) | PROPOSED | Cần review schema, điểm gọi và kiểm chứng liên module trước triển khai |
| D-011 | Triển khai CAT-01 quản lý loại sản phẩm trong Admin bằng `LOAISANPHAM`/DbContext hiện có; không đổi schema, authentication, layout chung hoặc storefront | APPROVED | Chỉ đạo CAT-01 của người dùng ngày 2026-11-18: tạo branch từ commit nền chỉ định, kiểm chứng, commit, push và tạo Draft PR vào `integration/foundation`; không merge hoặc chuyển CAT-02 trước review. Không phê duyệt toàn bộ D-008/D-009/D-010 |
| D-012 | Triển khai CAT-02 quản lý thương hiệu trong Admin bằng `THUONGHIEU`/DbContext hiện có, role `ADMIN`; ngừng sử dụng qua sửa trạng thái, không xóa cứng; `LogoUrl` là đường dẫn được kiểm tra, chưa upload | APPROVED | Chỉ đạo CAT-02 của người dùng ngày 2026-11-18 sau review tĩnh CAT-01: branch riêng từ `integration/foundation`, kiểm chứng, commit, push, tạo Draft PR vào nền rồi dừng review. Không merge CAT-01/CAT-02; không đổi schema/EDMX, storefront, authentication hoặc layout chung; `.csproj` chỉ đăng ký file mới. Không chạy SQL/seed khi chưa có database được chỉ định. Không phê duyệt toàn bộ D-008/D-009/D-010; D-011 thuộc PR CAT-01 độc lập, không kế thừa commit chưa merge |
| D-013 | Dùng duy nhất `NgoThanhDat` từ `integration/foundation` để thống nhất CAT-01/CAT-02 và làm việc catalog tiếp theo | APPROVED | Chỉ đạo CONSOLIDATE-DAT và điều chỉnh mới nhất ngày 2026-11-18: bảo toàn code/test đã review (gồm CAT-02 Review Fix 01), đủ đăng ký project và D-011/D-012; build/chạy cả hai bộ kiểm tra, commit/push `NgoThanhDat`. Sau xác minh remote đủ nội dung, đóng PR #1/#2 với liên kết branch mới và xóa branch cũ `feature/dat/catalog-categories`, `feature/dat/catalog-brands`, `feature/dat/catalog-management` nếu tồn tại. Không tạo PR mới, merge, force-push, xóa branch đang checkout hoặc làm mất untracked; giữ nguyên `integration/foundation`, `main`, `NguyenThanhNhan`, `DaoDuyTan`. Điều chỉnh này thay yêu cầu Draft PR tổng hợp và giữ branch cũ trước đó, thay quy trình branch/PR riêng tại D-011/D-012; không phê duyệt chức năng kế tiếp |
| D-014 | CAT-03 quản lý nhà cung cấp tại `/Admin/Suppliers` bằng `NHACUNGCAP`/DbContext hiện có, giữ quan hệ sản phẩm/phiếu nhập và ngừng sử dụng thay xóa cứng | APPROVED | Người dùng giao CAT-03 ngày 2026-11-18 trên `NgoThanhDat`: ADMIN, ViewModel, POST/anti-forgery, validation theo schema; chỉ mã là unique, không tự áp unique cho tên/liên hệ. Build/kiểm thử, cập nhật HANDOFF, commit/push và dừng review trước quản lý sản phẩm. Không tạo branch/PR, sửa schema/EDMX/auth/storefront/module Tân hoặc chạy SQL phá hủy dữ liệu; không phê duyệt các đề xuất tồn kho còn mở |

Điểm review tiếp theo: xử lý các mục OPEN/PROPOSED cần cho gói tích hợp kế tiếp; không coi kết thúc gói tài liệu là phê duyệt các mục đó.

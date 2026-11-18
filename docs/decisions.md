# Sổ quyết định

## Cách đọc

- **APPROVED**: được người dùng phê duyệt, không đồng nghĩa đã triển khai hoặc nghiệm thu runtime.
- **PROPOSED**: phương án kỹ thuật để review, chưa được phép triển khai chỉ nhờ có mặt trong tài liệu.
- **OPEN**: cần nhóm xác nhận chính sách hoặc trách nhiệm.

Nguồn duyệt hiện tại là chỉ đạo người dùng trong hội thoại FOUNDATION-01, FOUNDATION-02A và FOUNDATION-02B.1. Hai báo cáo đầu chưa được lưu thành artifact trong repository; không coi là tài liệu ký duyệt có sẵn. Mọi thay đổi trạng thái cần ghi căn cứ/người duyệt tại đây.

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

Điểm review tiếp theo: xử lý các mục OPEN/PROPOSED cần cho gói tích hợp kế tiếp; không coi kết thúc gói tài liệu là phê duyệt các mục đó.

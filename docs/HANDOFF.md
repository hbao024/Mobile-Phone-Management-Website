# Bàn giao FOUNDATION-02B.1

Snapshot ngày 2026-11-18, múi giờ Asia/Saigon. Quy trình khôi phục context nằm tại [AGENTS.md](../AGENTS.md); trạng thái duyệt nằm tại [decisions.md](decisions.md).

## Snapshot Git

| Đối tượng | Giá trị tại thời điểm kiểm tra |
|---|---|
| Branch làm việc | `integration/foundation` |
| SHA nền Nhàn / HEAD trước commit tài liệu | `a675fdcf712fcdb52f1d3a562c2a17857ee0850c` |
| Ref nguồn Nhàn | `origin/NguyenThanhNhan` |
| SHA nguồn Admin Tân | `4990488c1e85dacdf762eab817f3cceeb183171c` |
| Ref nguồn Tân | `origin/DaoDuyTan` |
| HEAD và main trước tạo branch | `313c7d20935f08c284261d20aaa4b675ca05773a` |
| Working tree / staging trước tạo branch | Sạch / sạch; không có file untracked |

HEAD sau commit phải đọc bằng `git rev-parse HEAD`; file này không chứa SHA của chính commit tạo ra nó. Các SHA remote là snapshot, cần đọc lại nếu nhiệm vụ tiếp theo phụ thuộc remote.

## Nhiệm vụ và phạm vi

Tạo bộ tài liệu nền để chuyển chat và chuẩn bị tích hợp. Diff được phép chỉ gồm năm file mới:

```text
AGENTS.md
docs/architecture.md
docs/contracts/catalog-inventory.md
docs/decisions.md
docs/HANDOFF.md
```

Source vẫn ở cấu trúc ứng viên; module Admin Tân chưa được nhập. Không sửa code, config, EDMX hoặc SQL, không chuyển thư mục. Không merge/push, không bắt đầu FOUNDATION tiếp theo.

## Tiến độ và kiểm chứng

- Đã kiểm tra Git sạch và xác minh SHA hai remote bằng `git ls-remote --heads origin`.
- Đã kiểm tra tên branch chưa tồn tại ở local/remote rồi tạo từ SHA nền cố định.
- Đã đọc instruction và kiểm tra source context/dependency; kết luận giới hạn ở kiểm tra tĩnh.
- Đã tạo đúng năm tài liệu; kiểm tra bằng Python đối chiếu danh sách thay đổi với allowlist và HEAD nền: PASS.
- Đã kiểm tra các liên kết file và anchor nội bộ trong cả năm tài liệu: PASS.
- Đã quét năm tài liệu bằng các mẫu dò phép gán mật khẩu, connection string và giá trị mật khẩu demo đã biết: không có kết quả khớp. Đây không phải audit bí mật toàn repository.
- Đã stage đúng năm file mới, kiểm `git diff --cached --check` thành công và review diff; không có file hiện hữu bị sửa. Snapshot này được ghi trước commit; cần kiểm lại status sau commit.
- Chưa chạy build, UI, SQL, seed hoặc thử nghiệm concurrency; không có bằng chứng runtime.

Nguồn Admin có thể đọc không đổi branch bằng `git show <ref-nguồn-Tân>:QuanLiDienThoaiDD/Areas/Admin/Controllers/OrdersController.cs` và đường dẫn tương tự tới `StockReceiptsController.cs`. Không sao chép dữ liệu nhạy cảm khi đọc source.

## Điểm tiếp tục

Commit dự kiến: `docs: establish foundation architecture and handoff`. Xác minh commit thực tế bằng `git log -1 --format=fuller`, phạm vi bằng `git diff --name-status <SHA-nền> HEAD`, và trạng thái sau commit bằng `git status --short --branch`. Kết quả sau commit nằm trong Git và báo cáo bàn giao, không tự gán trước SHA/kết quả vào snapshot.

Sau kiểm chứng và commit gói tài liệu, dừng để review. Bước kế tiếp cần chỉ đạo mới; các mục còn mở được quản lý tại sổ quyết định. Khi đổi chat, kiểm tra HEAD/status thực tế thay vì mặc định snapshot trước commit là trạng thái hiện tại.

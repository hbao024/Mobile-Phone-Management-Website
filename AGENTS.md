# Quy tắc làm việc

## Bắt đầu và khôi phục context

1. Đọc [HANDOFF](docs/HANDOFF.md) để biết nhiệm vụ, snapshot Git và điểm dừng.
2. Tự kiểm tra `git branch --show-current`, `git rev-parse HEAD`, `git status --short --branch`, `git diff --stat` và `git diff --cached --stat`.
3. Nếu thực tế khác snapshot, đọc các commit/diff phát sinh trước khi tiếp tục. Không tự reset hoặc checkout để ép trạng thái khớp tài liệu.
4. Đọc [sổ quyết định](docs/decisions.md), sau đó chỉ đọc kiến trúc/contract liên quan nhiệm vụ. Context bàn giao không tự cấp quyền triển khai bước tiếp theo.
5. Nếu nhiệm vụ phụ thuộc remote, xác minh lại SHA bằng `git ls-remote --heads origin`. Ghi rõ khi chưa xác minh được; không dùng snapshot cũ làm bằng chứng hiện tại.

## Thực hiện thay đổi

- Ưu tiên đúng đắn, kiểm chứng được và thay đổi nhỏ nhất đủ giải quyết yêu cầu.
- Đọc instruction gần nhất trước khi sửa. Nêu rủi ro cụ thể và hệ quả nếu yêu cầu hoặc phương án có vấn đề.
- Chỉ làm trong phạm vi đã duyệt; không tự thêm feature, service, dependency hoặc refactor rộng.
- Trước khi sửa file dùng chung, xem ranh giới module và ownership trong [kiến trúc](docs/architecture.md); xác định chủ module bị ảnh hưởng.
- Không sửa tay entity/context generated. Thay đổi schema hoặc cập nhật EDMX cần phạm vi được duyệt riêng; tránh nhiều người cập nhật EDMX đồng thời.
- Không chạy SQL, seed, reset môi trường, xóa dữ liệu, rewrite history hoặc force-push khi chưa có quyền rõ ràng. Giữ nguyên công việc hiện hữu.
- Không đưa mật khẩu, token hoặc connection string nhạy cảm vào tài liệu, commit hoặc đầu ra kiểm chứng.
- Các đề xuất chưa duyệt trong contract không phải yêu cầu được phép triển khai.

## Kiểm chứng và bàn giao

- Kiểm diff, staging và Git status; chạy kiểm tra phù hợp phạm vi được phép.
- Tách rõ kiểm tra tĩnh, build, UI, database và concurrency. Không ghi PASS cho phần chưa chạy.
- Trước bàn giao, cập nhật HANDOFF với nhiệm vụ, SHA lúc kiểm tra, thay đổi đang dở, bằng chứng và phần chưa xác minh.
- Quyết định mới hoặc thay đổi trạng thái duyệt chỉ ghi tại sổ quyết định; tài liệu chuyên môn dẫn mã quyết định.
- Quyền tạo/sửa file không mặc nhiên bao gồm commit, merge hoặc push; tuân theo chỉ đạo cụ thể của phiên làm việc.

## Nguồn thông tin duy nhất

| Nội dung | Tài liệu sở hữu |
|---|---|
| Quy trình làm việc, kiểm chứng, khôi phục context | File này |
| Stack, cấu trúc, ranh giới và ownership | [architecture.md](docs/architecture.md) |
| Nội dung contract sản phẩm, giá, tồn | [catalog-inventory.md](docs/contracts/catalog-inventory.md) |
| Trạng thái duyệt, căn cứ và quyết định | [decisions.md](docs/decisions.md) |
| Branch, SHA, Git status, nhiệm vụ và bằng chứng phiên | [HANDOFF.md](docs/HANDOFF.md) |

Các file còn lại chỉ dẫn liên kết hoặc mã quyết định, không chép lại nội dung thuộc nguồn khác.

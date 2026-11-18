# Bàn giao — CAT-05 / NgoThanhDat

Snapshot 2026-11-18 (Asia/Saigon). Quy trình: [AGENTS.md](../AGENTS.md); phạm vi tại D-016 trong [decisions.md](decisions.md), chi tiết tại [contract](contracts/catalog-inventory.md#ảnh-biến-thể-cat-05). Dừng để review, không chuyển bước tiếp theo.

## Git và phạm vi

- Tiếp tục `NgoThanhDat`, HEAD lúc kiểm tra trước commit CAT-05: `943ebffba6357f4664e317cef08ff95c2b198346` (commit CAT-04). Đã đọc commit/source thực tế và xác minh `git ls-remote --heads origin NgoThanhDat` cùng SHA lúc bắt đầu; snapshot CAT-04 ghi HEAD trước commit, không có thay đổi source phát sinh ngoài commit đó.
- Không tạo branch/PR, merge, force-push. `.vscode/settings.json` và `foundation-review.patch` untracked có sẵn được giữ nguyên, không commit.
- Thay đổi controller/ViewModel và ba view ProductVariants của Đạt; một helper ảnh trong Areas/Admin; `.csproj` (Hoàng Bảo điều phối) chỉ đăng ký helper, dùng System.Drawing sẵn có. Thêm tests/CAT-05, cập nhật contract/decisions/HANDOFF. Không sửa code Nhàn/Tân, storefront, auth/layout, schema/EDMX/generated, kho hoặc dependency.
- Phiên được giao commit/push lên `NgoThanhDat` rồi dừng review. SHA của commit chứa HANDOFF đọc lại từ Git, không tự gán trong file này; kiểm tra staging/status và remote sau push trong kết quả phiên.

## Hành vi và bằng chứng

- Upload JPEG/PNG từ Create/Edit, tên GUID, chuẩn hóa PNG, URL `/images/variants/...`; validation định dạng/MIME/nội dung, 2 MiB và giới hạn pixel. Hiển thị ảnh DB, preview, thay/bỏ ảnh; giữ đường dẫn ảnh cũ tương thích storefront. Chi tiết và chính sách giữ file cũ/lỗi bù tại contract, không tạo cơ chế dọn dữ liệu cũ.
- Build Debug solution bằng VS MSBuild `C:/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Current/Bin/MSBuild.exe`: thành công; chỉ còn cảnh báo binding MSB3247 hiện hữu.
- `powershell -NoProfile -ExecutionPolicy Bypass -File tests/CAT-05/Run.ps1`: **100/100** kiểm tra file thật/GDI+/gọi controller với DB giả; script preview và multipart đạt kiểm tra bằng Node với DOM giả. Gồm JPEG/PNG, tên traversal được thay bằng tên server, file rỗng/giả/hỏng/sai MIME/đuôi, quá byte/pixel, ContentLength sai, đúng 2 MiB, bỏ payload, 12 upload đồng thời trùng tên đầu vào, giữ ảnh dùng chung, thay/bỏ, lỗi ghi filesystem thật, lỗi DB/concurrency giả, commit-rồi-báo-lỗi, kết quả lưu không rõ dù không có tham chiếu DB và lỗi truy vấn/dọn file.
- Hồi quy bằng `tests/CAT-01/Run.ps1` **31/31**, CAT-02 **80/80**, CAT-03 **64/64**, CAT-04 **211/211**. Bao gồm parse/compile Razor, validation/overposting và metadata ADMIN/POST/anti-forgery. Không coi metadata là đã kiểm filter qua HTTP. Dữ liệu/file test ở thư mục `obj/Cat05Checks`, không dùng ảnh hoặc dữ liệu ứng dụng.

## Giới hạn cần review

- Project vẫn thiếu Web.config gốc, startup gọi seed, chưa có database kiểm thử an toàn được chỉ định: không chạy ứng dụng IIS/SQL/seed. **Chưa xác minh** UI trình duyệt/multipart HTTP, auth/CSRF thực tế, SQL persistence/transaction/collation/concurrency hoặc quyền ghi IIS và giới hạn request host.
- Cơ chế dọn file sau lỗi DB chỉ áp dụng khi xác nhận chưa lưu/rollback và không có tham chiếu; giữ file khi kết quả lưu không rõ hoặc không xác minh được. Không có transaction chung filesystem/SQL. Chưa nghiệm thu race tham chiếu/cleanup; crash tiến trình hoặc lỗi dọn có thể để file dư, cần đối soát riêng. Không tự dọn ảnh cũ vì giỏ hàng/session có thể còn dùng.
- Giữ các giới hạn CAT-04: chưa có concurrency token, sửa đồng thời có thể ghi đè; storefront/checkout chưa lọc đầy đủ trạng thái catalog; tạo biến thể không tự tạo TONKHO. Không xử lý các mục này trong CAT-05.

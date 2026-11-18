# Bàn giao — CAT-07 / NgoThanhDat

Snapshot 2026-11-18 (Asia/Saigon). Quy trình tại [AGENTS.md](../AGENTS.md), phê duyệt phạm vi xem tại D-018 và mục ghi chung còn mở D-019 trong [decisions.md](decisions.md). Dừng review.

## Git và phạm vi

- Branch NgoThanhDat, HEAD trước CAT-07 `ef8c318eded16d23bc98888145e9f1d467cf8218`, local/remote khớp lúc bắt đầu. Đã đọc commit CAT-06/source để đối chiếu snapshot ghi SHA trước commit.
- Đã fetch/kiểm tra remote DaoDuyTan `4990488c1e85dacdf762eab817f3cceeb183171c`, đọc Orders/StockReceipts và model tại SHA đó bằng git show, không checkout/merge hoặc sửa code Tân.
- Thêm controller/ViewModel/view Inventory của Đạt; thêm link từ Products; csproj (Hoàng Bảo điều phối) chỉ đăng ký hai C# và một view. Thêm hai file tests/CAT-07; cập nhật ba tài liệu hiện có. Không thêm service/dependency/tài liệu mới, sửa Nhàn/Tân/schema/EDMX/generated/auth/layout hoặc ghi kho.
- Giữ nguyên .vscode/settings.json và foundation-review.patch untracked có sẵn, không commit. Commit/push branch hiện tại theo chỉ đạo, không branch/PR/merge/force-push; SHA commit chứa file này đối chiếu Git/remote sau push trong kết quả phiên.

## Hoàn thiện và phần cần thống nhất

- Admin Inventory chỉ GET/ADMIN; xem biến thể/SKU/sản phẩm/hai trạng thái, tồn/đang đặt/ngày cập nhật, gồm biến thể thiếu TONKHO. Tìm/lọc/phân trang server; không tạo tồn, ghi log hoặc gọi SaveChanges.
- Chi tiết rà soát writer Tân và đầu vào/điểm tích hợp đề xuất tại [contract](contracts/catalog-inventory.md#xem-tồn-và-điểm-tích-hợp-cat-07), không coi tài liệu là phê duyệt writer.
- Chưa triển khai tăng/giảm tồn. Nhóm cần thống nhất routine ghi chung cho nhập, hai nhánh xác nhận đơn, hoàn và điều chỉnh; quyền/actor/lý do, chính sách thiếu TONKHO/DangDat, chống lặp/concurrency và transaction tồn + log. Không có writer thứ hai trong Inventory.

## Kiểm chứng và giới hạn

- VS MSBuild Debug solution thành công, chỉ MSB3247 binding hiện hữu.
- tests/CAT-07/Run.ps1: **33/33** với dữ liệu giả 45 biến thể, thiếu/có TONKHO/tồn 0, tồn < đang đặt giữ nguyên dữ liệu, ngày cập nhật, hai trạng thái, tìm kiếm/lọc/phân trang/tham số sai/rỗng, không tạo/sửa tồn; metadata chỉ có GET Index/ADMIN, Razor parse/compile và kiểm source giữ tham số pager.
- Trong bộ trên, **3** truy vấn EF6 thực sinh SQL offline gồm LEFT OUTER JOIN và phân trang với ba điều kiện bản ghi tồn. Dùng EDMX hiện tại, connection giả invalid.invalid và tắt initializer; không thực thi SQL hoặc startup/seed.
- Hồi quy thực chạy CAT-01 **31/31**, CAT-02 **80/80**, CAT-03 **64/64**, CAT-04 **211/211**, CAT-05 **100/100** + preview DOM giả, CAT-06 **31/31**. Không coi metadata/compile/sinh SQL là kiểm thử HTTP hoặc DB.
- Chưa có DB an toàn được chỉ định, thiếu Web.config gốc, startup gọi seed: không chạy IIS/app/SQL/seed. UI/auth HTTP, SQL thực thi/collation/query plan và concurrency/transaction liên module **chưa nghiệm thu**. Đối chiếu Tân chỉ là review source tại SHA ghi trên, chưa xác nhận tích hợp/runtime.

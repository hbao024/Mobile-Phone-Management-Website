# Contract sản phẩm, giá và tồn kho

Đây là mô tả source và nội dung để review, không phải một thay đổi database đã triển khai. Trạng thái phê duyệt của từng đề xuất nằm duy nhất trong [sổ quyết định](../decisions.md), đặc biệt D-005, D-008, D-009 và D-010.

## Hiện trạng từ source

Nguồn: [SQL hiện có](../../DoAnLTWeb/QL_HANGHOA_PHUKIEN_WEB.sql), [EDMX](../../DoAnLTWeb/QuanLiDienThoaiDD/QuanLiDienThoaiDD/Models/Model1.edmx), [checkout](../../DoAnLTWeb/QuanLiDienThoaiDD/QuanLiDienThoaiDD/Controllers/DatHangController.cs) và [giỏ hàng](../../DoAnLTWeb/QuanLiDienThoaiDD/QuanLiDienThoaiDD/Models/CartItem.cs). Đây là bằng chứng tĩnh, không xác nhận database đang chạy.

| Đối tượng | Dữ liệu hiện có |
|---|---|
| `SANPHAM` | Mã sản phẩm, thông tin chung, loại, thương hiệu, NCC mặc định, bảo hành và trạng thái |
| `BIENSANPHAM` | Mã biến thể, FK sản phẩm, SKU duy nhất, màu/RAM/bộ nhớ, giá và ảnh |
| Giá | `GiaBan`, `GiaNhap` dùng `decimal(18,2)`; ràng buộc không âm |
| `TONKHO` | Khóa theo biến thể, `SoLuongTon`, `SoLuongDangDat`, thời điểm cập nhật |
| `GIAODICHKHO` | Loại giao dịch, số lượng, tồn trước/sau và liên kết chứng từ/người thực hiện |
| Chi tiết đơn | Lưu tên, đơn giá, số lượng, giảm giá; thành tiền tính trong SQL |
| Chi tiết phiếu nhập | Lưu đơn giá và số lượng từng biến thể; thành tiền tính trong SQL |

Checkout hiện dùng giá đã lưu trong session và sinh mã theo số bản ghi. EDMX chưa có concurrency token. So sánh nguồn Admin ở FOUNDATION-02A cho thấy xử lý kho có transaction nhưng vẫn đọc rồi gán số tồn; giá nhập được cập nhật từ phiếu nhập. Nguồn Admin chưa nằm trong cây ứng viên; ref bằng chứng được ghi ở HANDOFF để đọc bằng `git show`, không cần checkout.

## Biên quản lý catalog CAT-04

Phạm vi và căn cứ tại D-015 trong [sổ quyết định](../decisions.md). Phần này mô tả quản trị catalog, không thay các đề xuất tồn kho bên dưới.

- Mã sản phẩm/biến thể và sản phẩm cha của biến thể giữ bất biến. SKU sửa được nhưng phải duy nhất toàn bảng, kể cả biến thể ngừng bán; không áp unique tên sản phẩm hoặc tổ hợp màu/RAM/bộ nhớ.
- Khi tạo/chọn mới, chỉ nhận loại, thương hiệu, NCC đang hoạt động. Khi sửa sản phẩm, cho giữ nguyên liên kết cũ đã ngừng sử dụng hoặc chuyển sang bản ghi đang hoạt động; NCC mặc định có thể để trống. Không xóa/sửa bản ghi danh mục hoặc lịch sử chứng từ.
- Sản phẩm và biến thể có trạng thái riêng, không cập nhật dây chuyền; Admin vẫn quản lý được biến thể dưới sản phẩm ngừng kinh doanh. Storefront/checkout chưa lọc đầy đủ trạng thái biến thể; CAT-04 không sửa phía khách.
- Giá nhập/bán nhận giá trị không âm trong decimal(18,2), tối đa 9999999999999999.99 và 2 chữ số thập phân; bảo hành 0–120 tháng, RAM/bộ nhớ tùy chọn nhưng phải từ 1–32767 nếu có. Không tính lại snapshot giá/tên trên đơn hoặc phiếu nhập, không thay giá đã nằm trong session giỏ hàng.
- Chỉ dùng `BIENSANPHAM.HinhAnhUrl` (tùy chọn, 500 ký tự). Storefront hiện ghép `~` với giá trị lưu, nên form nhận `/images/...`; thư mục/tên dùng chữ ASCII/số/_/-, tên ảnh được có dấu chấm. Chặn traversal, URL ngoài, ký tự điều khiển, backslash, dấu nháy và các ký tự không thuộc định dạng này. Đường dẫn nhập tay không xác minh file tồn tại; upload được bổ sung ở CAT-05 bên dưới. Không di chuyển/sửa các ảnh đã có trong repository.
- Tạo biến thể không tạo TONKHO hoặc nhật ký kho; cập nhật biến thể không nhận/ghi tồn, đơn hàng, giỏ hàng hoặc chi tiết nhập hàng. Luồng tồn/nhập của Tân cần được kiểm chứng riêng; không coi biến thể mới đã có tồn khả dụng.

## Ảnh biến thể CAT-05

Phạm vi và căn cứ tại D-016 trong [sổ quyết định](../decisions.md).

- Form thêm/sửa dùng multipart, giữ trường đường dẫn cũ, cho tải JPEG (.jpg/.jpeg) hoặc PNG. Server đối chiếu phần mở rộng, MIME và định dạng giải mã; đọc tối đa 2 MiB + 1 byte để phát hiện vượt giới hạn dù ContentLength sai. Ảnh tối đa 2 MiB đầu vào, 4096 pixel mỗi chiều và 16 triệu pixel; giải mã và mã hóa lại PNG để bỏ metadata/nội dung đính kèm. Không nhận SVG hoặc giữ file gốc do người dùng gửi.
- Lưu tại `images/variants/` trong web project MVC 5, tên GUID server sinh + `.png`, ghi bằng CreateNew để không ghi đè. `HinhAnhUrl` lưu `/images/variants/<guid>.png`, giữ tương thích các chỗ storefront ghép `~`. Chỉ tạo thư mục lúc có upload; tài khoản IIS cần quyền ghi tại đó. Không sửa config dùng chung; giới hạn request IIS/ASP.NET vẫn áp dụng trước controller, đặc biệt request lớn hơn giới hạn host.
- Ảnh hiện tại lấy từ DB; preview dùng object URL trên trình duyệt và thu hồi khi đổi/bỏ ảnh/rời trang. Preview không thay validation server. Upload mới ưu tiên hơn đường dẫn nhập; bỏ ảnh đặt URL null. Upload và bỏ ảnh đồng thời bị từ chối. Khi lưu lỗi phải chọn lại file.
- Giữ nguyên file ảnh cũ khi thay/bỏ, kể cả ảnh upload trước đó: biến thể khác, logo hoặc giỏ hàng trong session có thể còn dùng. Chưa có cơ chế thu gom file cũ; dung lượng có thể tăng, việc dọn cần đối soát riêng.
- Nếu ghi file lỗi, không gọi SaveChanges; dọn phần file đã tạo trong khả năng filesystem cho phép. Nếu lưu DB lỗi, chỉ thử xóa file mới khi lỗi xác nhận chưa lưu/rollback (EF validation, EF concurrency hoặc SQL duplicate 2601/2627), đồng thời truy vấn AsNoTracking xác nhận không có biến thể/logo local tham chiếu. Nếu kết quả lưu không rõ, truy vấn hoặc xóa thất bại thì giữ file và ghi Trace để đối soát. Không suy luận chưa từng commit chỉ từ việc không tìm thấy tham chiếu hiện tại: session có thể đã giữ URL. Không xóa ảnh cũ. Filesystem không cùng transaction SQL; lỗi tiến trình/host hoặc DB không xác minh được có thể để lại file dư. Race tham chiếu/cleanup liên request chưa được kiểm chứng trên SQL.

## Xem tồn và điểm tích hợp CAT-07

Phạm vi xem tồn tại D-018; cơ chế ghi chung cần chốt tại D-019 trong [sổ quyết định](../decisions.md). SHA branch Tân đã đối chiếu nằm tại HANDOFF; các đường dẫn/line dưới đây thuộc snapshot đó, không phải code đã nhập vào branch Đạt.

- `/Admin/Inventory` chỉ GET, ADMIN, bắt đầu từ BIENSANPHAM nối SANPHAM và left join TONKHO theo mã biến thể. Không có TONKHO thì số lượng/ngày là chưa có dữ liệu, không thay bằng 0 hoặc ngày hiện tại. Hiển thị riêng trạng thái sản phẩm/biến thể và hai số lượng lưu thực tế; không tính “khả dụng” khi chưa chốt nghĩa SoLuongDangDat. Không truy cập/ghi GIAODICHKHO để dựng số tồn.
- Tìm mã biến thể/SKU/mã hoặc tên sản phẩm, lọc hai trạng thái và có/chưa có TONKHO, phân trang server 20 dòng với thứ tự tên/mã sản phẩm/mã biến thể. Không tạo tồn để hiển thị. Tìm quá 200 ký tự trả 400; tham số lọc lạ về tất cả, trang sai về 1 hoặc kẹp trang cuối kèm thông báo. Kết quả đọc tại thời điểm truy vấn, không đảm bảo snapshot giữa Count/lấy trang hoặc giữa các request.
- Schema hiện có: TONKHO có PK/FK MaBienSanPham, hai số lượng int không âm và NgayCapNhat; không có ràng buộc DangDat <= Ton. GIAODICHKHO có identity bigint, loại NHAP/BAN/TRA_HANG/DIEU_CHINH_TANG/DIEU_CHINH_GIAM, số lượng dương, tồn trước/sau không âm, liên kết phiếu/đơn/NV và ghi chú. Đây là kiểm tra source SQL/EDMX, không phải schema DB runtime.

| Điểm ghi của Tân (`QuanLiDienThoaiDD/Areas/Admin/Controllers/`) | Hành vi hiện có từ source |
|---|---|
| `StockReceiptsController.Create` (266), `ConfirmReceipt` (406) → `ApplyStockReceiptToInventory` (505–555) | Chỉ DA_NHAP cộng tồn/ghi NHAP; thiếu TONKHO thì tạo 0 trước khi cộng; cập nhật ngày, GiaNhap nếu đơn giá >0. Phiếu/chi tiết/tồn/log cùng transaction. CHO_XU_LY chưa cộng; CancelReceipt (469) chỉ hủy phiếu chờ, không đảo phiếu đã nhập |
| `OrdersController.ProcessStatusTransition` (277–484) | CHO_XAC_NHAN → DA_XAC_NHAN kiểm đủ tồn, trừ tồn/ghi BAN; giảm DangDat nếu đủ. Hủy từ DA_XAC_NHAN hoặc DANG_GIAO cộng lại/ghi TRA_HANG, có tạo TONKHO nếu thiếu. Trạng thái/lịch sử/tồn/log cùng transaction |
| `OrdersController.Create` (499–701), nhánh XacNhanNgay (636–678) | Có kiểm tồn cả khi tạo đơn chờ; xác nhận ngay có nhánh trừ tồn/ghi BAN riêng, không giảm DangDat như ProcessStatusTransition. Tạo chờ chưa trừ/giữ tồn |

Các điểm cần nhóm chốt trước khi mở thao tác điều chỉnh (D-019, liên quan D-008/D-010):

- Tân và Đạt thống nhất một routine ghi dùng chung, bao phủ cả ba điểm ghi trên và điều chỉnh tương lai. Routine private hiện tại chỉ phục vụ nhập, chưa là API tích hợp. Cần phạm vi duyệt riêng để sửa/extract code Tân; không gọi chéo controller hoặc sao chép writer vào Inventory.
- Đầu vào đề xuất: mã biến thể, loại tăng/giảm, số lượng nguyên dương, lý do bắt buộc; actor/thời gian lấy server. Số tồn mới tính trên dữ liệu hiện tại, không nhận số tồn trước/sau từ form. Một transaction cập nhật TONKHO và thêm GIAODICHKHO với trước/sau, loại, actor, ngày, ghi chú; không sửa log cũ, giá nhập hoặc chứng từ để giả lập điều chỉnh.
- Chốt quyền điều chỉnh, chính sách khi thiếu TONKHO, nghĩa/vòng đời DangDat và điều kiện giảm khi đang giữ hàng; không tự reset DangDat. Chốt điều kiện hoàn khi DANG_GIAO và sự khác nhau giữa hai nhánh xác nhận đơn hiện tại.
- Chốt chống mất cập nhật (ghi có điều kiện/khóa/phiên bản), chống lặp request/xác nhận và rollback khi lỗi ở bất kỳ dòng/log nào. Transaction đọc-rồi-gán hiện có chưa chứng minh chống oversell/lost update; chưa có concurrency token. Nếu chọn đổi schema/EDMX phải duyệt riêng.
- Bằng chứng cần trước khi nghiệm thu writer: nhập đồng thời bán/điều chỉnh, tranh đơn vị cuối, xác nhận/hủy lặp và đồng thời, rollback nhiều dòng, lỗi commit không rõ, đối soát tồn trước/sau với log. CAT-07 không thực thi các nghiệp vụ đó.

## Vấn đề cần thống nhất

- Thời điểm chốt giá, đọc lại giá và yêu cầu xác nhận khi giá thay đổi.
- Vòng đời `SoLuongDangDat`: source phía khách chưa giữ hàng, còn nguồn Admin có giảm trường này khi xác nhận.
- Điều kiện xuất, hoàn kho và hủy khi đang giao.
- Ý nghĩa `GiaNhap`, tiêu chí biến thể trùng và ngừng bán.
- Quyền sửa giá/điều chỉnh kho và trách nhiệm giữa các module.

## Nội dung đề xuất cho D-008

- Khóa sản phẩm/biến thể bất biến; SKU duy nhất. Không đổi quan hệ biến thể đã có chứng từ tùy ý.
- Server đọc lại giá, trạng thái và số lượng tại checkout; giá session chỉ là giá đã hiển thị. Nhóm cần chọn thời điểm chốt và cách xác nhận giá mới.
- Giữ snapshot tên/giá trên chứng từ; cập nhật catalog không sửa lịch sử.
- Tạo bản ghi tồn bằng 0 cùng transaction với biến thể; điều chỉnh kho ghi chênh lệch và lý do.
- Ưu tiên ngừng hoạt động thay vì xóa dữ liệu đã có tham chiếu.
- Phương án ít thay đổi là chưa giữ hàng khi đặt, chỉ trừ khi xác nhận. Hệ quả: đơn chờ không bảo đảm còn hàng. Nếu chọn giữ hàng, phải xác định thời hạn và giải phóng khi hủy/hết hạn.
- Không tự sửa hoặc đưa `SoLuongDangDat` hiện hữu về 0; cần đối soát trước khi chọn thuật toán.
- Khi đã xuất kho, chỉ hoàn theo điều kiện nghiệp vụ đã chốt, đúng một lần. Hủy khi đang giao cần phân biệt hủy đơn với thực nhận lại hàng.
- Giữ nghĩa giá nhập tham khảo gần nhất là một phương án; không tự coi là giá vốn bình quân.
- Ảnh upload cần kiểm tra định dạng/kích thước, tên do server sinh, nơi lưu không thực thi script; xử lý bù nếu lưu DB thất bại vì filesystem không cùng transaction SQL.

## Nội dung đề xuất cho D-010

- Cân nhắc `rowversion` trên tồn, sản phẩm/biến thể và chứng từ; map concurrency token trong EDMX. Form sửa phải gửi phiên bản đã đọc, không chỉ tải bản mới rồi ghi đè.
- Một transaction bao trọn kiểm tra trạng thái/dòng hàng, cập nhật tồn, nhật ký kho, trạng thái và lịch sử chứng từ.
- Xung đột hoặc thiếu tồn ở một dòng phải rollback toàn nghiệp vụ; xử lý nhiều biến thể theo thứ tự khóa cố định.
- Chặn áp dụng lặp bằng trạng thái có kiểm soát đồng thời, ràng buộc nguồn nhật ký phù hợp và mã request duy nhất cho tạo đơn.
- Sinh mã có bảo đảm duy nhất từ database; không dựa riêng vào `Count()+1` hoặc `MAX+1`.
- Retry có giới hạn, đọc lại dữ liệu và chạy lại toàn transaction; không tự ghi đè dữ liệu cũ.

## Bằng chứng cần có khi triển khai contract

Hai context độc lập tranh đơn vị tồn cuối; xác nhận lặp; xác nhận đồng thời với hủy; nhập đồng thời với bán; nhiều dòng thiếu một dòng; lỗi trước commit; form sửa cũ; giá đổi trong giỏ; truy xuất chứng từ sau ngừng bán. Kiểm tra phải đối soát cả số tồn, trạng thái và nhật ký. Danh sách này là tiêu chí tương lai, chưa có kết quả chạy.
